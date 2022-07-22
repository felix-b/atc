using System.Diagnostics;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using FluentAssertions;
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
        var silo = SiloTestDoubles.CreateSilo("ABCD", ConfigureSilo, environment: environment);
        var world = silo.Grains.CreateGrain<WorldGrain>(
            id => new WorldGrain.GrainActivationEvent(id)
        );
        var groundQ = silo.Grains.CreateGrain<RadioStationGrain>(
            id => new RadioStationGrain.GrainActivationEvent(id, RadioStationType.Ground)
        );
        groundQ.Get().TurnOnGroundStation(Location.At(10f, 20f, 100f), Frequency.FromKhz(123000));

        groundQ.Get().OnTransceiverStateChanged += state => Console.WriteLine($"Q> status [{state.Status}]");
        groundQ.Get().OnIntentCaptured += intent => Console.WriteLine($"Q> captured [{intent}]");

        var groundOp = silo.Grains.CreateGrain<PocAIRadioOperatorGrain>(
            grainId => new PocAIRadioOperatorGrain.GrainActivationEvent(
                grainId, 
                "Q",
                groundQ.As<IRadioStationGrain>()
            ));
        
        var mobileOpA = CreateMobileStation(silo, "A");
        var mobileOpB = CreateMobileStation(silo, "B");
        var mobileOpC = CreateMobileStation(silo, "C");
        var mobileOpD = CreateMobileStation(silo, "D");

        //-- when
        
        int iterationCount = 0;
        while (silo.NextWorkItemAtUtc < DateTime.MaxValue)
        {
            iterationCount++;
            Console.WriteLine($"====== iteration #{iterationCount} ======");

            //Thread.Sleep(silo.NextWorkItemAtUtc - environment.UtcNow);
            environment.UtcNow = silo.NextWorkItemAtUtc;
            Console.WriteLine($"{environment.UtcNow:HH:mm:ss.fff}");

            silo.ExecuteReadyWorkItems();
        }

        //-- then 
        
        iterationCount.Should().Be(21);
        environment.UtcNow.Subtract(startUtc).Should().Be(TimeSpan.FromSeconds(39));
        
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

    [Test]
    public void ConversationEndToEndTest()
    {
        //-- given
        
        // var startUtc = new DateTime(2022, 10, 10, 8, 30, 0); 
        // var environment = new SiloTestDoubles.TestEnvironment {
        //     UtcNow = startUtc
        // };
        var startUtc = DateTime.UtcNow;
        var environment = new SiloTestDoubles.TestEnvironment(); // use real date/time
        var silo = SiloTestDoubles.CreateSilo("ABCD", ConfigureSilo, environment: environment);
        var world = silo.Grains.CreateGrain<WorldGrain>(
            id => new WorldGrain.GrainActivationEvent(id)
        );
        var groundQ = silo.Grains.CreateGrain<RadioStationGrain>(
            id => new RadioStationGrain.GrainActivationEvent(id, RadioStationType.Ground)
        );
        groundQ.Get().TurnOnGroundStation(Location.At(10f, 20f, 100f), Frequency.FromKhz(123000));

        groundQ.Get().OnTransceiverStateChanged += state => Console.WriteLine($"Q> status [{state.Status}]");
        groundQ.Get().OnIntentCaptured += intent => Console.WriteLine($"Q> captured [{intent}]");

        var groundOp = silo.Grains.CreateGrain<PocAIRadioOperatorGrain>(
            grainId => new PocAIRadioOperatorGrain.GrainActivationEvent(
                grainId, 
                "Q",
                groundQ.As<IRadioStationGrain>()
            ));
        
        var mobileOpA = CreateMobileStation(silo, "A");
        var mobileOpB = CreateMobileStation(silo, "B");
        var mobileOpC = CreateMobileStation(silo, "C");
        var mobileOpD = CreateMobileStation(silo, "D");

        //-- when
        
        int iterationCount = 0;
        while (silo.NextWorkItemAtUtc < DateTime.MaxValue)
        {
            iterationCount++;
            Console.WriteLine($"====== iteration #{iterationCount} ======");

            Thread.Sleep(silo.NextWorkItemAtUtc - environment.UtcNow);
            Console.WriteLine($"{environment.UtcNow:HH:mm:ss.fff}");

            silo.ExecuteReadyWorkItems();
        }

        //-- then 
        
        iterationCount.Should().Be(21);
        environment.UtcNow.Subtract(startUtc).TotalSeconds.Should().BeApproximately(39.0d, precision: 1.0d);
        
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
    
    private GrainRef<PocAIRadioOperatorGrain> CreateMobileStation(ISilo silo, string callsign)
    {
        var station = silo.Grains.CreateGrain<RadioStationGrain>(
            id => new RadioStationGrain.GrainActivationEvent(id, RadioStationType.Mobile)
        );

        station.Get().TurnOnMobileStation(Location.At(10f, 20f, 1000f), Frequency.FromKhz(123000));
        
        var aiOperator = silo.Grains.CreateGrain<PocAIRadioOperatorGrain>(
            grainId => new PocAIRadioOperatorGrain.GrainActivationEvent(
                grainId, 
                callsign,
                station.As<IRadioStationGrain>()
            ));

        return aiOperator;
    }

    private void ConfigureSilo(SiloConfigurationBuilder config)
    {
        WorldGrain.RegisterGrainType(config);
        RadioStationGrain.RegisterGrainType(config);
        GroundStationRadioMediumGrain.RegisterGrainType(config);
        PocAIRadioOperatorGrain.RegisterGrainType(config);
    }
}
