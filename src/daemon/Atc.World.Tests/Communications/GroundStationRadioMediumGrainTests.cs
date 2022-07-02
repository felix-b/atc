using Atc.Grains;
using Atc.World.Communications;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Atc.World.Tests.Communications;

[TestFixture]
public class GroundStationRadioMediumGrainTests
{
    //--- initialization ---
    
    // can create grain through activation (how will station-medium association work?)
    [Test]
    public void CanActivate()
    {
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);

        var groundStation = new Mock<IRadioStationGrain>();
        groundStation.SetupGet(x => x.StationType).Returns(RadioStationType.Ground);
        
        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(
                grainId, 
                GroundStation: SiloTestDoubles.MockGrainRef(groundStation.Object)));
        
        mediumGrain.Get().GroundStation.Get().Should().BeSameAs(groundStation.Object);
    }

    // Given: --  
    //  When: grain was just activated
    //  Then: current state is silence since activation, all collections empty
    [Test]
    public void CanInitializeEmptyState()
    {
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = DateTime.UtcNow
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);

        var groundStation = new Mock<IRadioStationGrain>();
        groundStation.SetupGet(x => x.StationType).Returns(RadioStationType.Ground);
        
        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(
                grainId, 
                GroundStation: SiloTestDoubles.MockGrainRef(groundStation.Object)));

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.IsSilent.Should().BeTrue();
        state.SilenceSinceUtc.Should().Be(environment.UtcNow);
        state.MobileStationById.Should().BeEmpty();
        state.ConversationsInProgress.Should().BeEmpty();
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.TransmittingStationIds.Should().BeEmpty();
        state.GroundStation.Get().Should().BeSameAs(groundStation.Object);
    }


    //--- add/remove stations ---

    // Given: silence;
    //        no mobile stations tuned  
    //  When: a mobile station MS#1 is added
    //  Then: state contains MS#1
    //        AddListener invoked on MS#1 

    // Given: silence;
    //        2 mobile stations MS#1 and MS#2 are tuned  
    //  When: MS#1 is removed 
    //  Then: state does not contain MS#1;
    //        state does contain MS#2;
    //        RemoveListener invoked on MS#1  

    // Given: MS#1 is tuned;
    //        MS#1 is transmitting;
    //  When: MS#2 is added
    //  Then: NotifyTransmissionStarted invoked on MS#2 

    // Given: MS#1 and MS#2 is tuned;
    //        MS#1 is transmitting;
    //  When: MS#2 is removed
    //  Then: NotifyTransmissionAborted invoked on MS#2 

    //--- begin/end transmissions ---

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 registered for transmission;
    //        silence;
    //  When: MS#1 begins to transmit
    //  Then: state: contains transmission and transmitting station id
    //        state: conversation moved from pending to in-progress
    //        notify: NotifyTransmissionStarted invoked on MS#2, GS; 

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        transmission designated end of conversation
    //  When: MS#1 completes transmission;
    //  Then: state: transmission and transmitting station id removed
    //        state: conversation removed
    //        NotifyTransmissionCompleted invoked on MS#2, GS; 

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting
    //  When: MS#1 aborts transmission
    //  Then: state: transmission and transmitting station id removed
    //        state: conversation removed
    //        NotifyTransmissionAborted invoked on MS#2, GS; 

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting
    //  When: MS#2 starts transmission
    //  Then: state: transmission and transmitting station id added
    //        state: conversation moved from pending to in-progress
    //        NotifyTransmissionStarted invoked on MS#1, GS; 

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        MS#2 transmitting
    //  When: MS#2 aborts transmission
    //  Then: NotifyTransmissionAborted invoked on MS#1, GS; 

    // Given: MS#1 and MS#2 tuned;
    //        silence;
    //  When: GS starts transmission
    //  Then: NotifyTransmissionStarted invoked on MS#1, MS#2

    // Given: MS#1 and MS#2 tuned;
    //        GS transmitting;
    //  When: GS completes transmission
    //  Then: NotifyTransmissionCompleted invoked on MS#1, MS#2

    // Given: MS#1 and MS#2 tuned;
    //        GS transmitting;
    //  When: GS aborts transmission
    //  Then: NotifyTransmissionAborted invoked on MS#1, MS#2

    //--- pending transmission priority queue ---
    
    //-(1) Ground station? goes first
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //  When: GS/C3 enqueued
    //  Then: pending order: (1) GS/C3, (2) MS1/C1, (3) MS2/C3 
    
    //-(2) Transmission related to an in-progress conversation? goes first
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C3  
    //  When: MS3/C3 enqueued
    //  Then: pending order: (1) MS3/C3, (2) MS1/C1, (3) MS2/C3 
    
    //-(3) Compare AirGroundPriority
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //        C1=Urgency, C2=FlightSafetyNormal  
    //  When: MS3/C3 enqueued, C3=DirectionFinding
    //  Then: pending order: (1) MS1/C1, (2) MS3/C3, (3) MS2/C2 
    
    //-(4) Compare Ids (chronological order)
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //        C1=FlightSafetyNormal, C2=FlightSafetyNormal  
    //  When: MS3/C3 enqueued, C3=FlightSafetyNormal
    //  Then: pending order: (1) MS1/C1, (2) MS2/C2, (3) MS3/C3 

    //-(5) No duplicate entries
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //        C1=FlightSafetyNormal, C2=FlightSafetyNormal  
    //  When: MS2/C2=FlightSafetyNormal enqueued
    //  Then: pending order: (1) MS1/C1, (2) MS2/C2 

    //-(6) Update priority of conversation
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C0  
    //        C1=FlightSafetyNormal, C2=FlightSafetyNormal  
    //  When: MS2/C2=FlightSafetyHigh enqueued
    //  Then: pending order: (1) MS2/C2=FlightSafetyHigh, (2) MS1/C1=FlightSafetyNormal 

    //--- state reduction for BeginTransmitNow ---
    
    //-(1) new in-progress conversation: remove from pending queue and add to in-progress
    // Given: pending order: (1) MS1/C1, MS2/C2 
    //        in-progress conversations: none  
    //  When: ready to transmit  
    //  Then: invoke BeginTransmitNow on waiting station operator;
    //        remove MS1/C1 from pending queue;
    //        add C1 to in-progress conversations
        
    //-(2) continue in-progress conversation: no duplicates in in-progress
    // Given: pending order: (1) MS1/C1, MS2/C2 
    //        in-progress conversations: C1  
    //  When: ready to transmit  
    //  Then: invoke BeginTransmitNow on waiting station operator;
    //        remove MS1/C1 from pending queue;
    //        in-progress conversations unchanged: C1

    //------- Silence duration rules ---------
    
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: none  
    //  When: determining delay before transmission
    //  Then: delay 0 before MS1/C1

    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //  When: determining delay before transmission
    //  Then: delay 0 before MS1/C1

    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C3  
    //  When: determining delay before transmission
    //  Then: delay before MS1/C1 according to C1's AigGroundPriority

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
    // Script -->   Pending:         In-Progress
    //   A!#*       [A#1           ] [              ]  
    //   B!#*       [A#1,B#2       ] [              ]
    //   AV#1       [B#2           ] [#1            ]
    //   A->Q#1     [B#2           ] [#1            ]
    //   Q!#1       [Q#1,B#2       ] [#1            ]
    //   QV#1       [B#2           ] [#1            ]
    //   Q->A#1X    [B#2           ] [              ]
    //   C!#*       [B#2,C#3       ] [              ]
    //   ---wait-silence---
    //   BV#2       [C#3           ] [#2            ]
    //   B->Q#2     [C#3           ] [#2            ]
    //   Q!#*       [Q#4,C#3       ] [#2            ]
    //   QV#4       [C#3           ] [#2,#4         ]
    //   Q->D#4     [C#3           ] [#2,#4         ]
    //   D!#4       [D#4,C#3       ] [#2,#4         ]
    //   DV#4       [C#3           ] [#2,#4         ]
    //   D->Q#4X    [C#3           ] [#2            ]
    //   Q!#2       [Q#2,C#3       ] [#2            ]
    //   QV#2       [C#3           ] [#2            ]
    //   Q->B#2     [C#3           ] [#2            ]
    //   B!#2       [B#2,C#3       ] [#2            ]
    //   BV#2       [C#3           ] [#2            ]
    //   B->Q#2     [C#3           ] [#2            ]
    //   Q!#3       [Q#3,C#3       ] [#2            ]
    //   QV#3       [C#3           ] [#2,#3         ]
    //   Q->C#3     [C#3           ] [#2,#3         ] 
    //   C!#3       [C#3           ] [#2,#3         ] -- C#3 already in pending, not duplicated
    //   CV#3       [              ] [#2,#3         ]
    //   C->Q#3X    [              ] [#2            ]
    //   Q!#2       [Q#2           ] [#2            ]
    //   QV#2       [              ] [#2            ]
    //   Q->B#2X    [              ] [              ]


    private void ConfigureSiloForTest(SiloConfigurationBuilder config)
    {
        RadioStationGrain.RegisterGrainType(config);
        GroundStationRadioMediumGrain.RegisterGrainType(config);
    }
}
