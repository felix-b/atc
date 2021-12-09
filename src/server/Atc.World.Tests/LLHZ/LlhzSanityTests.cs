using System;
using System.Collections.Generic;
using System.Linq;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.LLHZ;
using FluentAssertions;
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
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId)
            );
            
            setup.RunWorldIterations(TimeSpan.FromSeconds(1), 1);

            llhz.Get().Should().NotBeNull();
            setup.Supervisor.GetAllActorsOfType<LlhzAirportActor>().Count().Should().Be(1);
            setup.Supervisor.GetAllActorsOfType<AircraftActor>().Count().Should().Be(1);
            setup.Supervisor.GetAllActorsOfType<LlhzDeliveryControllerActor>().Count().Should().Be(1);
            setup.Supervisor.GetAllActorsOfType<LlhzPilotActor>().Count().Should().Be(1);
        }

        [Test]
        public void CanStartupForPatternFlight()
        {
            var intentLog = new List<Intent>();
            
            var setup = new WorldSetup(enableInspectableLogs: true);
            setup.AddIntentListener(intentLog.Add);
            
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId)
            );

            var pilot = llhz.Get().GetChildrenOfType<LlhzPilotActor>().First().Get();
            var clearanceController = llhz.Get().GetChildrenOfType<LlhzDeliveryControllerActor>().First().Get();

            setup.RunWorldIterations(TimeSpan.FromSeconds(1), 120);

            var stateTransiitonLog = setup.GetLogEntries()
                .Where(e => e.Id == "AIRadioOperatingActor.ActorTransitionedState")
                .Select(e => $"{e.Time} : {e.KeyValuePairs["actorId"]} : {e.KeyValuePairs["oldState"]}->{e.KeyValuePairs["trigger"]}->{e.KeyValuePairs["newState"]}")
                .ToArray();

            intentLog.Select(intent => intent.GetType()).Should().BeStrictlyEquivalentTo(
                typeof(GreetingIntent),
                typeof(GoAheadInstructionIntent),
                typeof(StartupRequestIntent),
                typeof(StartupApprovalIntent),
                typeof(StartupApprovalReadbackIntent)
            );
        }
    }
}