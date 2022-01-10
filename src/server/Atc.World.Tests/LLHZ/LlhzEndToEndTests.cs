using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.Speech.AzurePlugin;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.LLHZ;
using Atc.World.Testability;
using FluentAssertions;
using NUnit.Framework;
using Zero.Doubt.Logging;

namespace Atc.World.Tests.LLHZ
{
    [TestFixture, Category("e2e")]
    public class LlhzEndToEndTests
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
        public void StartupForPatternFlight()
        {
            var setup = new WorldSetup(logType: WorldSetup.LogType.BinaryStream);
            var radioLog = new List<string>();
            var intentLog = new List<string>();
            
            using var audioContext = new AudioContextScope(setup.ResolveDependency<ISoundSystemLogger>());
            
            setup.DependencyContextBuilder
                .WithSingleton<ISpeechSynthesisPlugin>(new AzureSpeechSynthesisPlugin())
                .WithSingleton<IRadioSpeechPlayer>(new RadioSpeechPlayer(setup.Environment))
                .WithSingleton<IVerbalizationService>(new LlhzVerbalizationService(setup.Environment), replace: true);
            
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId)
            );

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(130850), 
                new GeoPoint(32.18d, 34.83d), 
                Altitude.FromFeetMsl(1000), 
                callsign: "air1");
            airStation1.Get().AddListener((station, status, intent) => {
                radioLog.Add($"{setup.World.Get().Timestamp.ToString(@"hh\:mm\:ss\.fff")}:{status}:{intent?.GetType()?.Name ?? "no-intent"}");
                if (intent != null)
                {
                    intentLog.Add($"{intent.Header.OriginatorCallsign}->{intent.Header.RecipientCallsign}:{intent.GetType().Name}");
                }
            }, out _);
            airStation1.Get().PowerOn();
 
            using var monitor = new RadioStationSoundMonitor(
                setup.Supervisor,
                setup.ResolveDependency<IVerbalizationService>(),
                setup.ResolveDependency<ISpeechSynthesisPlugin>(),
                setup.ResolveDependency<IRadioSpeechPlayer>(),
                setup.ResolveDependency<ICommsLogger>(),
                airStation1.Get());

            setup.RunWorldFastForward(TimeSpan.FromSeconds(1), 57);
            setup.RunWorldRealTime(TimeSpan.FromMilliseconds(100), 570);

            intentLog.Should().BeStrictlyEquivalentTo(new[] {
                $"4XCGK->Hertzlia Clearance:{nameof(GreetingIntent)}",
                $"Hertzlia Clearance->4XCGK:{nameof(GoAheadIntent)}",
                $"4XCGK->Hertzlia Clearance:{nameof(StartupRequestIntent)}",
                $"Hertzlia Clearance->4XCGK:{nameof(StartupApprovalIntent)}",
                $"4XCGK->Hertzlia Clearance:{nameof(StartupApprovalReadbackIntent)}",
                $"Hertzlia Clearance->4XCGK:{nameof(MonitorFrequencyIntent)}",
                $"4XCGK->Hertzlia Clearance:{nameof(MonitorFrequencyReadbackIntent)}",                
            });
            
            intentLog.ForEach(Console.WriteLine);
            var logRootNode = setup.ReadBinaryLogStream();
            var logTreeText = BinaryLogStreamTextPrinter.PrintTree(logRootNode);
            Console.WriteLine(logTreeText);
        }
    }
}