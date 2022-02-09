using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Control;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.LLHZ;
using Atc.World.Testability;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;

namespace Atc.World.Tests.LLHZ
{
    [TestFixture]
    public class LlhzSanityTests
    {
        private LlhzBufferContext _bufferContext = null;
        
        [OneTimeSetUp]
        public void BeforeAll()
        {
            _bufferContext = new();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            _bufferContext.Dispose();
        }

        [Test]
        public void CanCreateLlhzAirport()
        {
            var setup = new WorldSetup();
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId, AircraftCount: 1)
            );
            
            setup.RunWorldFastForward(TimeSpan.FromSeconds(1), 1);

            llhz.Get().Should().NotBeNull();
            setup.Supervisor.GetAllActorsOfType<LlhzAirportActor>().Count().Should().Be(1);
            setup.Supervisor.GetAllActorsOfType<Traffic.AircraftActor>().Count().Should().Be(1);
            setup.Supervisor.GetAllActorsOfType<LlhzControllerActor>().Count().Should().Be(2);
            setup.Supervisor.GetAllActorsOfType<LlhzPilotActor>().Count().Should().Be(1);
        }

        [Test]
        public void CanStartupForPatternFlight()
        {
            var clearanceIntentLog = new List<Intent>();
            
            WorldSetup setup = new(logType: WorldSetup.LogType.Inspectable);
            setup.AddIntentListener(intent => {
                if (IsClearanceDelivery(intent.CallsignCalling) || IsClearanceDelivery(intent.CallsignReceivingOrThrow()))
                {
                    clearanceIntentLog.Add(intent);
                }
            });
            
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId, AircraftCount: 1)
            );

            var pilot = llhz.Get().GetChildrenOfType<LlhzPilotActor>().First().Get();
            var clearanceController = llhz.Get().GetChildrenOfType<LlhzControllerActor>().First().Get();

            setup.RunWorldFastForward(TimeSpan.FromSeconds(1), 120);

            var stateTransiitonLog = setup.GetInspectableLogEntries()
                .Where(e => e.Id == "AIRadioOperatingActor.ActorTransitionedState")
                .Select(e => $"{e.Time} : {e.KeyValuePairs["actorId"]} : {e.KeyValuePairs["oldState"]}->{e.KeyValuePairs["trigger"]}->{e.KeyValuePairs["newState"]}")
                .ToArray();

            clearanceIntentLog.Select(intent => intent.GetType()).Should().BeStrictlyEquivalentTo(
                typeof(GreetingIntent),
                typeof(GoAheadIntent),
                typeof(StartupRequestIntent),
                typeof(StartupApprovalIntent),
                typeof(StartupApprovalReadbackIntent),
                typeof(MonitorFrequencyIntent),
                typeof(MonitorFrequencyReadbackIntent)
            );

            bool IsClearanceDelivery(string callsign)
            {
                return (callsign == "Hertzlia Clearance");
            }
        }
    }
}