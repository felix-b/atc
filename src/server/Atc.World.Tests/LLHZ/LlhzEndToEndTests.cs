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

            using var audioContext = SetupAudioAndSpeech(setup); 
            
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId, AircraftCount: 1)
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
        
        [Test]
        public void StartupAndFlyPatternFlight()
        {
            var setup = new WorldSetup(logType: WorldSetup.LogType.BinaryStream);
            var allRadiosLog = new List<string>();
            var clearanceIntentLog = new List<string>();
            var towerIntentLog = new List<string>();
            
            using var audioContext = SetupAudioAndSpeech(setup); 
            
            var llhz = setup.Supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId, AircraftCount: 3)
            );

            var airStation1 = setup.AddAirStation(
                Frequency.FromKhz(130850), 
                new GeoPoint(32.18d, 34.83d), 
                Altitude.FromFeetMsl(1000), 
                callsign: "air1");
            airStation1.Get().AddListener((station, status, intent) => {
                allRadiosLog.Add($"{setup.World.Get().Timestamp.ToString(@"hh\:mm\:ss\.fff")}:{status}:{intent?.GetType()?.Name ?? "no-intent"}");
                if (intent != null)
                {
                    clearanceIntentLog.Add($"{intent.Header.OriginatorCallsign}->{intent.Header.RecipientCallsign}:{intent.GetType().Name}");
                }
            }, out _);
            airStation1.Get().PowerOn();

            var airStation2 = setup.AddAirStation(
                Frequency.FromKhz(122200), 
                new GeoPoint(32.18d, 34.83d), 
                Altitude.FromFeetMsl(1000), 
                callsign: "air2");
            airStation2.Get().AddListener((station, status, intent) => {
                allRadiosLog.Add($"{setup.World.Get().Timestamp.ToString(@"hh\:mm\:ss\.fff")}:{status}:{intent?.GetType()?.Name ?? "no-intent"}");
                if (intent != null)
                {
                    towerIntentLog.Add($"{intent.Header.OriginatorCallsign}->{intent.Header.RecipientCallsign}:{intent.GetType().Name}");
                }
            }, out _);
            airStation2.Get().PowerOn();

            using var clearanceMonitor = new RadioStationSoundMonitor(
                setup.Supervisor,
                setup.ResolveDependency<IVerbalizationService>(),
                setup.ResolveDependency<ISpeechSynthesisPlugin>(),
                setup.ResolveDependency<IRadioSpeechPlayer>(),
                setup.ResolveDependency<ICommsLogger>(),
                airStation1.Get());
            using var towerMonitor = new RadioStationSoundMonitor(
                setup.Supervisor,
                setup.ResolveDependency<IVerbalizationService>(),
                setup.ResolveDependency<ISpeechSynthesisPlugin>(),
                setup.ResolveDependency<IRadioSpeechPlayer>(),
                setup.ResolveDependency<ICommsLogger>(),
                airStation2.Get());
            
            setup.RunWorldFastForward(TimeSpan.FromSeconds(1), 57);
            setup.RunWorldRealTime(TimeSpan.FromMilliseconds(100), 5000);

            Console.Write("----- CLRDEL INTENT LOG ------");
            clearanceIntentLog.ForEach(Console.WriteLine);
            Console.Write("----- TWR INTENT LOG ------");
            towerIntentLog.ForEach(Console.WriteLine);

            var logRootNode = setup.ReadBinaryLogStream();
            var logTreeText = BinaryLogStreamTextPrinter.PrintTree(logRootNode);
            Console.WriteLine(logTreeText);

            // clearanceIntentLog.Should().BeStrictlyEquivalentTo(new[] {
            //     $"4XCGK->Hertzlia Clearance:{nameof(GreetingIntent)}",
            //     $"Hertzlia Clearance->4XCGK:{nameof(GoAheadIntent)}",
            //     $"4XCGK->Hertzlia Clearance:{nameof(StartupRequestIntent)}",
            //     $"Hertzlia Clearance->4XCGK:{nameof(StartupApprovalIntent)}",
            //     $"4XCGK->Hertzlia Clearance:{nameof(StartupApprovalReadbackIntent)}",
            //     $"Hertzlia Clearance->4XCGK:{nameof(MonitorFrequencyIntent)}",
            //     $"4XCGK->Hertzlia Clearance:{nameof(MonitorFrequencyReadbackIntent)}",                
            // });
            //
            // towerIntentLog.Should().BeStrictlyEquivalentTo(new[] {
            //     $"4XCGK->Hertzlia:{nameof(DepartureTaxiRequestIntent)}",
            //     $"Hertzlia->4XCGK:{nameof(DepartureTaxiClearanceIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(DepartureTaxiClearanceReadbackIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(ReportReadyForDepartureIntent)}",
            //     $"Hertzlia->4XCGK:{nameof(TakeoffClearanceIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(TakeoffClearanceReadbackIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(ReportDownwindIntent)}",
            //     $"Hertzlia->4XCGK:{nameof(LandingSequenceAssignmentIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(LandingSequenceAssignmentReadbackIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(FinalApproachReportIntent)}",
            //     $"Hertzlia->4XCGK:{nameof(LandingClearanceIntent)}",
            //     $"4XCGK->Hertzlia:{nameof(LandingClearanceReadbackIntent)}",
            // });
        }

        AudioContextScope SetupAudioAndSpeech(WorldSetup setup)
        {
            setup.DependencyContextBuilder
                .WithSingleton<ISpeechSynthesisPlugin>(new AzureSpeechSynthesisPlugin())
                .WithTransient<IRadioSpeechPlayer>(() => new RadioSpeechPlayer(setup.Environment))
                .WithSingleton<IVerbalizationService>(new LlhzVerbalizationService(setup.Environment), replace: true);
            
            return new AudioContextScope(setup.ResolveDependency<ISoundSystemLogger>());
        }
    }
}