using Atc.Grains;
using Atc.Maths;
using Atc.Sound.OpenAL;
using Atc.Speech.AzurePlugin;
using Atc.Telemetry;
using Atc.Telemetry.CodePath;
using Atc.Telemetry.Exporters.CodePath;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using FluentAssertions;
using GeneratedCode;
using NUnit.Framework;

namespace Atc.World.Tests.Communications.Poc;

[TestFixture]
public class PocConversationTests
{
    //--- Scenario Test --- 
    // Legend:
    //   !  = enqueue-ai-for-transmission
    //   V  = begin-transmit-now
    //   -> = transmit
    //   #* = new conversation, not yet assigned id
    //   #N = conversation-id N
    //   X  = transmission designated end of conversation
    //   A, B, C, D = mobile-stations
    //   Q  = ground-station
    // Script -->                 Pending:         In-Progress      Intent : Duration
    //   +00:00.000 | A!#*       [A#1           ] [              ]  
    //   +00:00.000 | B!#*       [A#1,B#2       ] [              ]
    //   +00:01.000 | AV#1       [B#2           ] [#1            ]
    //   +00:01.000 | A->Q#1     [B#2           ] [#1            ]  I1     : 00:03.000 
    //   +00:04.000 | Q!#1       [Q#1,B#2       ] [#1            ]
    //   +00:04.000 | QV#1       [B#2           ] [#1            ]
    //   +00:04.000 | Q->A#1X    [B#2           ] [              ]  I2 X   : 00:04.000
    //   +00:05.000 | C!#*       [B#2,C#3       ] [              ]  -- has I11 to transmit, but since it first receives I8 from Q, it transmits I9 instead 
    //   ---wait-silence---
    //   +00:09.000 | BV#2       [C#3           ] [#2            ]
    //   +00:09.000 | B->Q#2     [C#3           ] [#2            ]  I3     : 00:05.000
    //   +00:14.000 | Q!#*       [Q#4,C#3       ] [#2            ]
    //   +00:14.000 | QV#4       [C#3           ] [#2,#4         ]
    //   +00:14.000 | Q->D#4     [C#3           ] [#2,#4         ]  I4     : 00:04.000
    //   +00:18.000 | D!#4       [D#4,C#3       ] [#2,#4         ]
    //   +00:18.000 | DV#4       [C#3           ] [#2,#4         ]
    //   +00:18.000 | D->Q#4X    [C#3           ] [#2            ]  I5 X   : 00:03.000
    //   +00:21.000 | Q!#2       [Q#2,C#3       ] [#2            ]
    //   +00:21.000 | QV#2       [C#3           ] [#2            ]
    //   +00:21.000 | Q->B#2     [C#3           ] [#2            ]  I6     : 00:02.000
    //   +00:23.000 | B!#2       [B#2,C#3       ] [#2            ]
    //   +00:23.000 | BV#2       [C#3           ] [#2            ]
    //   +00:23.000 | B->Q#2     [C#3           ] [#2            ]  I7     : 00:03.000
    //   +00:26.000 | Q!#3       [Q#3,C#3       ] [#2            ]
    //   +00:26.000 | QV#3       [C#3           ] [#2,#3         ]
    //   +00:26.000 | Q->C#3     [C#3           ] [#2,#3         ]  I8     : 00:04.000
    //   +00:30.000 | C!#3       [C#3           ] [#2,#3         ] -- C#3 already in pending, not duplicated
    //   +00:30.000 | CV#3       [              ] [#2,#3         ]
    //   +00:30.000 | C->Q#3X    [              ] [#2            ]  I9 X   : 00:05.000
    //   +00:35.000 | Q!#2       [Q#2           ] [#2            ]
    //   +00:35.000 | QV#2       [              ] [#2            ]
    //   +00:35.000 | Q->B#2X    [              ] [              ]  I10 X  : 00:04.000

    [Test]
    public void ConversationUnitTest()
    {
        //-- given
        
        var startUtc = new DateTime(2022, 10, 10, 8, 30, 0); 
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = startUtc
        };
        var silo = SiloTestDoubles.CreateSilo("ABCD", config => ConfigureSilo(config), environment: environment);
        var world = silo.Grains.CreateGrain<WorldGrain>(
            id => new WorldGrain.GrainActivationEvent(id)
        );
        var groundQ = silo.Grains.CreateGrain<RadioStationGrain>(
            id => new RadioStationGrain.GrainActivationEvent(id, RadioStationType.Ground, Callsign("G"))
        );
        groundQ.Get().TurnOnGroundStation(Location.At(10f, 20f, 100f), Frequency.FromKhz(123000));

        groundQ.Get().TransceiverStateChanged += state => Console.WriteLine($"Q> status [{state.Status}]");
        groundQ.Get().IntentCaptured += intent => Console.WriteLine($"Q> captured [{intent}]");

        var groundOp = silo.Grains.CreateGrain<PocAIControllerGrain>(
            grainId => new PocAIControllerGrain.AIControllerGrainActivationEvent(
                grainId, 
                new Callsign("Q", "Q"),
                world.As<IWorldGrain>(),
                groundQ.As<IRadioStationGrain>()
            )
        );
        
        var mobileOpA = CreateMobileStation(silo, world, "A");
        var mobileOpB = CreateMobileStation(silo, world, "B");
        var mobileOpC = CreateMobileStation(silo, world, "C");
        var mobileOpD = CreateMobileStation(silo, world, "D");

        //-- when
        
        int iterationCount = 0;
        while (silo.NextWorkItemAtUtc < DateTime.MaxValue)
        {
            iterationCount++;
            Console.WriteLine($"====== iteration #{iterationCount} ======");

            environment.UtcNow = silo.NextWorkItemAtUtc;
            Console.WriteLine($"{environment.UtcNow:HH:mm:ss.fff}");

            silo.ExecuteReadyWorkItems();
        }

        //-- then 

        iterationCount.Should().Be(20);
        environment.UtcNow.Subtract(startUtc).Should().Be(TimeSpan.FromSeconds(42));
        
        var mediumState = SiloTestDoubles.GetGrainState(
            groundQ.Get().GroundStationMedium!.Value.As<GroundStationRadioMediumGrain>().Get());
        mediumState.PendingTransmissionQueue.Should().BeEmpty();
        mediumState.InProgressTransmissionByStationId.Should().BeEmpty();
        mediumState.ConversationsInProgress.Should().BeEmpty();

        AssertPocIntentLog(
            groundOp.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I3", "I5", "I7", "I9"});
        AssertPocIntentLog(
            mobileOpA.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I2", "I3", "I4", "I5", "I6", "I7", "I8", "I9", "I10"});
        AssertPocIntentLog(
            mobileOpB.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I2", "I4", "I5", "I6", "I8", "I9", "I10"});
        AssertPocIntentLog(
            mobileOpC.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I2", "I3", "I4", "I5", "I6", "I7", "I8", "I10"});
        AssertPocIntentLog(
            mobileOpD.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I2", "I3", "I4", "I6", "I7", "I8", "I9", "I10"});
    }

    [Test, Category("manual")]
    public void ConversationEndToEndTest()
    {
        //-- given

        using var telemetryExporter = new CodePathWebSocketExporter(listenPortNumber: 3003);
        var telemetryEnvironment = new CodePathEnvironment(LogLevel.Debug, telemetryExporter);

        Thread.Sleep(5000);
        
        var startUtc = DateTime.UtcNow;
        var siloEnvironment = new SiloTestDoubles.TestEnvironment(); 
        var silo = SiloTestDoubles.CreateSilo(
            "ABCD", 
            config => ConfigureSilo(config, telemetryEnvironment), 
            environment: siloEnvironment,
            telemetry: AtcGrainsTelemetry.CreateCodePathTelemetry<ISiloTelemetry>(telemetryEnvironment));
        var myTelemetry = AtcWorldTestsTelemetry.CreateCodePathTelemetry<IMyTestTelemetry>(telemetryEnvironment);
            
        siloEnvironment.SetAssetRootPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "..", "assets"));

        using var audioContext = new AudioContextScope(
            AtcSoundOpenALTelemetry.CreateCodePathTelemetry<AudioContextScope.IMyTelemetry>(telemetryEnvironment));
        
        var verbalizationService = new PocVerbalizationService();
        var audioStreamCache = new PocAudioStreamCache();
        var speechSynthesisPlugin = new AzureSpeechSynthesisPlugin(
            audioStreamCache, 
            AtcSpeechAzurePluginTelemetry.CreateCodePathTelemetry<AzureSpeechSynthesisPlugin.IMyTelemetry>(telemetryEnvironment));
        var speechService = new SpeechService(
            verbalizationService,
            speechSynthesisPlugin,
            telemetry: AtcWorldTelemetry.CreateCodePathTelemetry<SpeechService.IMyTelemetry>(telemetryEnvironment));
        var radioSpeechPlayer = new OpenalRadioSpeechPlayer(siloEnvironment);
        
        var world = silo.Grains.CreateGrain<WorldGrain>(
            id => new WorldGrain.GrainActivationEvent(id)
        );
        var groundQ = silo.Grains.CreateGrain<RadioStationGrain>(
            id => new RadioStationGrain.GrainActivationEvent(id, RadioStationType.Ground, Callsign("Q"))
        );
        groundQ.Get().TurnOnGroundStation(Location.At(10f, 20f, 100f), Frequency.FromKhz(123000));

        groundQ.Get().TransceiverStateChanged += state => Console.WriteLine($"Q> status [{state.Status}]");
        groundQ.Get().IntentCaptured += intent => Console.WriteLine($"Q> captured [{intent}]");

        var mobileOpA = CreateMobileStation(silo, world, "A");
        var mobileOpB = CreateMobileStation(silo, world, "B");
        var mobileOpC = CreateMobileStation(silo, world, "C");
        var mobileOpD = CreateMobileStation(silo, world, "D");
        var groundOp = silo.Grains.CreateGrain<PocAIControllerGrain>(
            grainId => new PocAIControllerGrain.AIControllerGrainActivationEvent(
                grainId, 
                new Callsign("Q", "Q"),
                world.As<IWorldGrain>(),
                groundQ.As<IRadioStationGrain>()
            ));
        
        var monitor = new RadioStationSoundMonitor(
            silo,
            speechService,
            audioStreamCache,
            radioSpeechPlayer,
            telemetry: AtcWorldTelemetry.CreateCodePathTelemetry<RadioStationSoundMonitor.IMyTelemetry>(telemetryEnvironment),
            radioStation: groundQ.As<IRadioStationGrain>());

        //-- when
        
        int iterationCount = 0;
        while (silo.NextWorkItemAtUtc < DateTime.MaxValue)
        {
            iterationCount++;
            Console.WriteLine($"====== iteration #{iterationCount} ======");

            // var millisecondsToSleep = (int)silo.NextWorkItemAtUtc.Subtract(siloEnvironment.UtcNow).TotalMilliseconds;
            // myTelemetry.VerboseSleepBeforeNextWorkItem(nextWorkItemAtUtc: silo.NextWorkItemAtUtc, millisecondsToSleep);    
            //
            // if (millisecondsToSleep > 25)
            // {
            //     Thread.Sleep(millisecondsToSleep);
            // }

            silo.BlockWhileIdle(CancellationToken.None);
            
            Console.WriteLine($"{siloEnvironment.UtcNow:HH:mm:ss.fff}");
            silo.ExecuteReadyWorkItems();
        }

        //-- then 

        iterationCount.Should().BeGreaterOrEqualTo(20);
        siloEnvironment.UtcNow.Subtract(startUtc).TotalSeconds.Should().BeApproximately(37, precision: 2);
        
        var mediumState = SiloTestDoubles.GetGrainState(
            groundQ.Get().GroundStationMedium!.Value.As<GroundStationRadioMediumGrain>().Get());
        mediumState.PendingTransmissionQueue.Should().BeEmpty();
        mediumState.InProgressTransmissionByStationId.Should().BeEmpty();
        mediumState.ConversationsInProgress.Should().BeEmpty();

        AssertPocIntentLog(
            groundOp.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I3", "I5", "I7", "I9"});
        AssertPocIntentLog(
            mobileOpA.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I2", "I3", "I4", "I5", "I6", "I7", "I8", "I9", "I10"});
        AssertPocIntentLog(
            mobileOpB.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I2", "I4", "I5", "I6", "I8", "I9", "I10"});
        AssertPocIntentLog(
            mobileOpC.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I2", "I3", "I4", "I5", "I6", "I7", "I8", "I10"});
        AssertPocIntentLog(
            mobileOpD.Get().Brain.AllReceivedIntentsLog,
            expectedIntentsInOrder: new[] {"I1", "I2", "I3", "I4", "I6", "I7", "I8", "I9", "I10"});
    }
    
    private void AssertPocIntentLog(IEnumerable<Intent> intentLog, params string[] expectedIntentsInOrder)
    {
        intentLog
            .OfType<PocIntent>()
            .Select(x => x.PocType.ToString())
            .Should().BeEquivalentTo(expectedIntentsInOrder, config: options => options.WithStrictOrdering());
    }
    
    private GrainRef<PocAIPilotGrain> CreateMobileStation(ISilo silo, GrainRef<WorldGrain> world, string callsign)
    {
        var station = silo.Grains.CreateGrain<RadioStationGrain>(
            id => new RadioStationGrain.GrainActivationEvent(id, RadioStationType.Mobile, Callsign(callsign))
        );

        station.Get().TurnOnMobileStation(Location.At(10f, 20f, 1000f), Frequency.FromKhz(123000));
        
        var aiOperator = silo.Grains.CreateGrain<PocAIPilotGrain>(
            grainId => new PocAIPilotGrain.AIPilotGrainActivationEvent(
                grainId, 
                new Callsign(callsign, callsign),
                world.As<IWorldGrain>(),
                station.As<IRadioStationGrain>()
            ));

        return aiOperator;
    }

    private void ConfigureSilo(SiloConfigurationBuilder config, CodePathEnvironment? codePathEnvironment = null)
    {
        TestUtility.RegisterTelemetryProvider(config, codePathEnvironment);
        WorldGrain.RegisterGrainType(config);
        RadioStationGrain.RegisterGrainType(config);
        GroundStationRadioMediumGrain.RegisterGrainType(config);
        PocAIPilotGrain.RegisterGrainType(config);
        PocAIControllerGrain.RegisterGrainType(config);
    }

    private Callsign Callsign(string text)
    {
        return new Callsign(text, text);
    }

    [TelemetryName("ConversationEndToEndTest")]
    public interface IMyTestTelemetry : ITelemetry
    {
        void VerboseSleepBeforeNextWorkItem(DateTime nextWorkItemAtUtc, int millisecondsToSleep);
    }
}
