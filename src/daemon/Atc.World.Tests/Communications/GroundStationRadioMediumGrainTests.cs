using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
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
        
        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(grainId)
        );

        mediumGrain.Get().GroundStation.CanGet.Should().BeFalse();
    }

    [Test]
    public void CanInitGroundStation()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(grainId)
        );
        var groundStationLocation = GroundLocation.Create(30, 30, 100); 
        var groundStation = MockGroundStation(
            frequency: Frequency.FromKhz(123000),
            location: groundStationLocation);

        //-- when

        mediumGrain.Get().InitGroundStation(groundStation.Grain);

        //-- then
        
        mediumGrain.Get().GroundStation.Should().Be(groundStation.Grain);
        mediumGrain.Get().AntennaLocation.Should().Be(groundStationLocation);
        mediumGrain.Get().Frequency.Should().Be(Frequency.FromKhz(123000));
        
        groundStation.Mock.Verify(x => x.AddListener(
            mediumGrain.As<IRadioStationListener>(), 
            RadioStationListenerMask.Transmitter));
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

        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(grainId)
        );

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.IsSilent.Should().BeTrue();
        state.SilenceSinceUtc.Should().Be(environment.UtcNow);
        state.MobileStationById.Should().BeEmpty();
        state.ConversationsInProgress.Should().BeEmpty();
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
        state.GroundStation.CanGet.Should().BeFalse();
    }

    //--- add/remove stations ---

    // Given: silence;
    //        no mobile stations tuned  
    //  When: a mobile station MS#1 is added
    //  Then: state contains MS#1
    //        AddListener invoked on MS#1 
    [Test]
    public void CanAddMobileStation()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(grainId)
        );
        var groundStation = MockGroundStation(Frequency.FromKhz(123000));
        mediumGrain.Get().InitGroundStation(groundStation.Grain);

        var mobileStation1 = MockMobileStation(Frequency.FromKhz(123000));
        
        //-- when

        mediumGrain.Get().AddMobileStation(mobileStation1.Grain);        
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.MobileStationById.Count.Should().Be(1);
        state.MobileStationById[mobileStation1.Grain.GrainId].Should().Be(mobileStation1.Grain);

        mobileStation1.Mock.Verify(x => x.AddListener(
            mediumGrain.As<IRadioStationListener>(), 
            RadioStationListenerMask.Transmitter));
    }

    // Given: silence;
    //        2 mobile stations MS#1 and MS#2 are tuned  
    //  When: MS#1 is removed 
    //  Then: state does not contain MS#1;
    //        state does contain MS#2;
    //        RemoveListener invoked on MS#1  
    [Test]
    public void CanRemoveMobileStation()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo,
            mobileStationCount: 2,
            out var ground,
            out var mobiles);
        
        //-- when

        mediumGrain.Get().RemoveMobileStation(mobiles[0].Station.Grain);        
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.MobileStationById.Count.Should().Be(1);
        state.MobileStationById[mobiles[1].Station.Grain.GrainId].Should().Be(mobiles[1].Station.Grain);

        mobiles[0].Station.Mock.Verify(x => x.RemoveListener(
            mediumGrain.As<IRadioStationListener>()
        ));
    }

    //--- register AI for transmissions ---
    
    // Given: MS#1 and MS#2 tuned;
    //        silence;
    //        no in-progress conversations;
    //        no pending transmission requests
    //  When: MS#1 registers AI for transmission
    //  Then: state: contains request in pending transmission queue 
    //        state: in-progress conversations collection is empty
    [Test]
    public void CanEnqueueAIOperatorForTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        //-- when

        var conversationToken = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHigh);
        
        //-- then

        conversationToken.Id.Should().Be(1);
        conversationToken.Priority.Should().Be(AirGroundPriority.FlightSafetyHigh);
        
        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.MobileStationById.Keys.Should().BeEquivalentTo(new[] {
            mobiles[0].Station.Grain.GrainId,
            mobiles[1].Station.Grain.GrainId,
        });

        state.IsSilent.Should().BeTrue();
        state.PendingTransmissionQueue.Count.Should().Be(1);
        state.PendingTransmissionQueue.First().Token.Should().BeSameAs(conversationToken);
        state.PendingTransmissionQueue.First().Operator.Should().Be(mobiles[0].Operator.Grain);
        state.ConversationsInProgress.Should().BeEmpty();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
    }

    //--- begin/end transmissions ---

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 registered for transmission;
    //        silence;
    //  When: MS#1 begins to transmit
    //  Then: state: contains transmission and transmitting station id
    //        state: conversation moved from pending to in-progress
    //        notify: NotifyTransmissionStarted invoked on MS#2, GS; 
    [Test]
    public void CanHandleBeginOfTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        
        //-- when

        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeFalse();
        state.InProgressTransmissionByStationId.Values
            .Select(e => e.StationTransmitting.GrainId).Should().BeEquivalentTo(new[] {
                mobiles[0].Station.Grain.GrainId
            });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] { conversationToken1 });
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.TransmissionWasInterfered.Should().BeFalse();

        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
        
        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);

        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>()
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        transmission does not designate end of conversation
    //  When: MS#1 completes transmission;
    //  Then: state: transmission and transmitting station id removed
    //        state: pending conversation is retained 
    //        NotifyTransmissionCompleted invoked on MS#2, GS; 
    [Test]
    public void CanHandleCompletionOfTransmission()
    {
        //-- given

        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(20);
        var intent1 = new IntentDescription(ConcludesConversation: false);

        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1,
            transmittedIntent: intent1);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeTrue();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] { conversationToken1 });
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.SilenceSinceUtc.Should().Be(environment.UtcNow);

        ground.Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>(),
            It.IsAny<IntentDescription>()
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        transmission designated end of conversation
    //  When: MS#1 completes transmission;
    //  Then: state: transmission and transmitting station id removed
    //        state: conversation removed
    //        NotifyTransmissionCompleted invoked on MS#2, GS; 
    [Test]
    public void CanHandleCompletionOfTransmissionEndingConversation()
    {
        //-- given

        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(20);
        var intent1 = new IntentDescription(ConcludesConversation: true);

        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1,
            transmittedIntent: intent1);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeTrue();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
        state.ConversationsInProgress.Should().BeEmpty();
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.SilenceSinceUtc.Should().Be(environment.UtcNow);

        ground.Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>(),
            It.IsAny<IntentDescription>()
        ), Times.Never);
    }
    
    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting
    //  When: MS#1 aborts transmission
    //  Then: state: transmission and transmitting station id removed
    //        state: conversation removed
    //        NotifyTransmissionAborted invoked on MS#2, GS; 
    [Test]
    public void CanHandleAbortedTransmission()
    {
        //-- given

        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(20);
        mediumGrain.Get().NotifyTransmissionAborted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeTrue();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
        state.ConversationsInProgress.Should().BeEmpty();
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.SilenceSinceUtc.Should().Be(environment.UtcNow);
        state.TransmissionWasInterfered.Should().BeFalse();

        ground.Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>()
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting
    //  When: MS#2 starts transmission (interfering with MS#1)
    //  Then: state: transmission and transmitting station id added
    //        state: conversation moved from pending to in-progress
    //        state: transmission interference flag is set
    //        NotifyTransmissionStarted invoked on MS#1, GS; 
    [Test]
    public void CanHandleBeginOfInterferingTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Operator.Grain);

        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        var transmission2 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeFalse();
        state.TransmissionWasInterfered.Should().BeTrue();
        state.InProgressTransmissionByStationId.Values
            .Select(e => e.StationTransmitting.GrainId).Should().BeEquivalentTo(new[] {
                mobiles[0].Station.Grain.GrainId,
                mobiles[1].Station.Grain.GrainId
            });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1,
            conversationToken2
        });
        state.PendingTransmissionQueue.Should().BeEmpty();

        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        MS#2 transmitting (interfering with MS#1's transmission)
    //  When: MS#2 completes transmission (conversation not concluded)
    //  Then: state: MS#2's transmission and transmitting station id removed
    //        state: MS#2's in-progress conversation retained
    //        state: transmission interference flag is set
    //        NotifyTransmissionAborted for MS#2's transmission invoked on MS#1, GS; 
    [Test]
    public void OverlappingTransmissions_InterferingTransmissionCompletes_NotifyAborted()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Operator.Grain);

        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var transmission2 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);

        //-- when
        
        var intent2 = new IntentDescription(ConcludesConversation: false);
        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2,
            transmittedIntent: intent2);

        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeFalse();
        state.TransmissionWasInterfered.Should().BeTrue();
        state.InProgressTransmissionByStationId.Values
            .Select(e => e.StationTransmitting.GrainId).Should().BeEquivalentTo(new[] {
                mobiles[0].Station.Grain.GrainId,
            });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1,
            conversationToken2
        });
        state.PendingTransmissionQueue.Should().BeEmpty();

        ground.Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Once);
        ground.Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            intent2
        ), Times.Never);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Once);
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            intent2
        ), Times.Never);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Never);
        mobiles[1].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            intent2
        ), Times.Never);
    }
    
    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        MS#2 transmitting, interfering MS#1's transmission
    //        MS#2 completed transmission  
    //  When: MS#1 completes transmission (conversation concluded)
    //  Then: state: MS#1's transmission and transmitting station id removed
    //        state: MS#1's in-progress conversation retained (despite intent specifying conversation concluded) 
    //        state: transmission interference flag is unset (because all transmissions ended)
    //        NotifyTransmissionAborted for MS#1's transmission invoked on MS#2, GS; 
    [Test]
    public void OverlappingTransmissions_InterferedTransmissionCompletes_NotifyAborted()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Operator.Grain);

        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var transmission2 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);

        var intent2 = new IntentDescription(ConcludesConversation: false);
        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2,
            transmittedIntent: intent2);

        //-- when
        
        var intent1 = new IntentDescription(ConcludesConversation: true);
        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1,
            transmittedIntent: intent1);

        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeTrue();
        state.TransmissionWasInterfered.Should().BeFalse();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1,
            conversationToken2
        });
        state.PendingTransmissionQueue.Should().BeEmpty();

        ground.Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
        ground.Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Never);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Never);
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Never);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
        mobiles[1].Station.Mock.Verify(x => x.EndReceiveCompletedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            intent1
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting;
    //        MS#2 transmitting (interfering with MS#1's transmission)
    //  When: MS#2 aborts transmission
    //  Then: state: MS#2's transmission and transmitting station id removed
    //        state: MS#2's in-progress conversation removed
    //        state: transmission interference flag is set
    //        NotifyTransmissionAborted invoked on MS#1, GS; 
    [Test]
    public void OverlappingTransmissions_InterferingTransmissionAborted_NotifyAborted()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Operator.Grain);

        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var transmission2 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);

        //-- when
        
        mediumGrain.Get().NotifyTransmissionAborted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);

        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeFalse();
        state.TransmissionWasInterfered.Should().BeTrue();
        state.InProgressTransmissionByStationId.Values
            .Select(e => e.StationTransmitting.GrainId).Should().BeEquivalentTo(new[] {
                mobiles[0].Station.Grain.GrainId
            });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1
        });
        state.PendingTransmissionQueue.Should().BeEmpty();

        ground.Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        silence;
    //  When: GS starts transmission
    //  Then: NotifyTransmissionStarted invoked on MS#1, MS#2
    [Test]
    public void CanHandleBeginOfTransmissionByGroundStation()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        
        //-- when

        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: ground.Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeFalse();
        state.InProgressTransmissionByStationId.Values
            .Select(e => e.StationTransmitting.GrainId).Should().BeEquivalentTo(new[] {
                ground.Station.Grain.GrainId 
            });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] { conversationToken1 });
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.TransmissionWasInterfered.Should().BeFalse();

        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>()
        ), Times.Never);
        
        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            ground.Station.Grain
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            ground.Station.Grain
        ), Times.Once);
    }

    // Given: MS#1 and MS#2 tuned;
    //        GS transmitting;
    //  When: GS completes transmission
    //  Then: NotifyTransmissionCompleted invoked on MS#1, MS#2
    //TODO - redundant?

    // Given: MS#1 and MS#2 tuned;
    //        GS transmitting;
    //  When: GS aborts transmission
    //  Then: NotifyTransmissionAborted invoked on MS#1, MS#2
    //TODO - redundant?

    // Given: MS#1 is tuned;
    //        MS#1 is transmitting;
    //  When: MS#2 is added
    //  Then: NotifyTransmissionStarted invoked on MS#2 
    [Test]
    public void CanNotifyNewlyAddedStationOnInProgressTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 1, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        var addedMobile = new MockedRadioStation(
            MockMobileStation(frequency: Frequency.FromKhz(123000)),
            Operator: TestUtility.MockGrain<IAIRadioOperatorGrain>()
        );
        mediumGrain.Get().AddMobileStation(addedMobile.Station.Grain);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        
        state.MobileStationById.Keys.Should().BeEquivalentTo(new[] {
           mobiles[0].Station.Grain.GrainId,
           addedMobile.Station.Grain.GrainId
        });

        addedMobile.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
    }

    // Given: MS#1 and MS#2 is tuned;
    //        MS#1 is transmitting;
    //  When: MS#2 is removed
    //  Then: NotifyTransmissionAborted invoked on MS#2 
    [Test]
    public void CanNotifyRemovedStationOnAbortOfInProgressTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Operator.Grain);
        var transmission1 = new TransmissionDescription();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        var removedMobile = mobiles[1];
        mediumGrain.Get().RemoveMobileStation(mobiles[1].Station.Grain);
        
        //-- then

        removedMobile.Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain
        ), Times.Once);
    }

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

    private GrainRef<GroundStationRadioMediumGrain> SetupGroundStationRadioMediumGrain(
        ISilo silo,
        int mobileStationCount,
        out MockedRadioStation ground,
        out List<MockedRadioStation> mobiles)
    {
        ground = new MockedRadioStation(
            Station: MockGroundStation(frequency: Frequency.FromKhz(123000)),
            Operator: TestUtility.MockGrain<IAIRadioOperatorGrain>()
        ); 
        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(grainId => 
            new GroundStationRadioMediumGrain.GrainActivationEvent(grainId)
        );
        mediumGrain.Get().InitGroundStation(ground.Station.Grain);

        mobiles = new List<MockedRadioStation>();
        for (int i = 0; i < mobileStationCount; i++)
        {
            var mobileItem = new MockedRadioStation(
                MockMobileStation(frequency: Frequency.FromKhz(123000)),
                Operator: TestUtility.MockGrain<IAIRadioOperatorGrain>()
            ); 
            mediumGrain.Get().AddMobileStation(mobileItem.Station.Grain);
            mobiles.Add(mobileItem);
        }

        return mediumGrain;
    }

    private MockedGrain<IRadioStationGrain> MockGroundStation(
        Frequency frequency = default,
        GroundLocation location = default)
    {
        var radioStation = TestUtility.MockGrain<IRadioStationGrain>();

        radioStation.Mock.SetupGet(x => x.StationType).Returns(RadioStationType.Ground);
        radioStation.Mock.SetupGet(x => x.GroundLocation).Returns(location);
        radioStation.Mock.SetupGet(x => x.TunedFrequency).Returns(frequency);

        return radioStation;
    }

    private MockedGrain<IRadioStationGrain> MockMobileStation(Frequency frequency = default)
    {
        var radioStation = TestUtility.MockGrain<IRadioStationGrain>();

        radioStation.Mock.SetupGet(x => x.StationType).Returns(RadioStationType.Mobile);
        radioStation.Mock.SetupGet(x => x.GroundLocation).Returns((GroundLocation?)null);
        radioStation.Mock.SetupGet(x => x.TunedFrequency).Returns(frequency);

        return radioStation;
    }

    private record MockedRadioStation(
        MockedGrain<IRadioStationGrain> Station,
        MockedGrain<IAIRadioOperatorGrain> Operator
    );
    
    // private static class TestTransceiverStates
    // {
    //     public static TransceiverState Silence(bool pttPressed = false)
    //     {
    //         return new TransceiverState(
    //             TransceiverStatus.Silence,
    //             PttPressed: pttPressed,
    //             Transmission: null,
    //             ConversationToken: null);
    //     }
    //
    //     public static TransceiverState Transmitting(ConversationToken token, TransmissionDescription? transmission = null)
    //     {
    //         return new TransceiverState(
    //             TransceiverStatus.Transmitting,
    //             PttPressed: true,
    //             Transmission: transmission ?? new TransmissionDescription(),
    //             ConversationToken: token);
    //     }
    // }
}
