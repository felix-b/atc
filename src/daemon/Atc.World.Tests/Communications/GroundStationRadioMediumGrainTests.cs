using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using FluentAssertions;
using FluentAssertions.Equivalency;
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
        var groundStationLocation = Location.At(30, 30, 100); 
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

    [Test]
    public void CanAssignConversationTokensConsequentIds()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        //-- when

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHigh);

        var conversationToken2 = mediumGrain.Get().TakeNewAIConversationToken();

        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain,
            mobiles[1].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHigh);
        
        //-- then

        conversationToken1.Id.Should().Be(1);
        conversationToken2.Id.Should().Be(2);
        conversationToken3.Id.Should().Be(3);
    }

    [Test]
    public void CanKeepAssigningConsequentIdsAfterEnqueueWithExistingToken()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        //-- when

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHigh);

        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            ground.Station.Grain,
            ground.Operator.Grain, 
            conversationToken: conversationToken1,
            AirGroundPriority.GroundToAir);

        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain,
            mobiles[1].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHigh);
        
        //-- then

        conversationToken1.Id.Should().Be(1);
        conversationToken2.Id.Should().Be(1);
        conversationToken3.Id.Should().Be(2);
    }

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
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHigh);
        
        //-- then

        conversationToken.Id.Should().Be(1);
        
        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.MobileStationById.Keys.Should().BeEquivalentTo(new[] {
            mobiles[0].Station.Grain.GrainId,
            mobiles[1].Station.Grain.GrainId,
        });

        state.IsSilent.Should().BeTrue();
        state.PendingTransmissionQueue.Count.Should().Be(1);
        state.PendingTransmissionQueue.First().Token.Should().BeSameAs(conversationToken);
        state.PendingTransmissionQueue.First().Operator.Should().Be(mobiles[0].Operator.Grain);
        state.PendingTransmissionQueue.First().Priority.Should().Be(AirGroundPriority.FlightSafetyHigh);
        state.ConversationsInProgress.Should().BeEmpty();
        state.InProgressTransmissionByStationId.Should().BeEmpty();
    }
    
    // Given: MS#1 and MS#2 tuned;
    //        silence 1min;
    //        no in-progress conversations;
    //        no pending transmission requests
    //  When: MS#1 registers AI for transmission
    //  Then: task queue has immediate work item to check pending transmissions    
    [Test]
    public void LongSilence_EnqueueAIForTransmission_ScheduleCheckPendingImmediate()
    {
        //-- given
        
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);

        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        silo.NextWorkItemAtUtc.Should().Be(DateTime.MaxValue);
        SiloTestDoubles.GetWorkItemsInTaskQueue(silo).Should().BeEmpty();
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddMinutes(1);
        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyNormal);
        
        //-- then

        silo.NextWorkItemAtUtc.Should().Be(environment.UtcNow);
        SiloTestDoubles.GetWorkItemsInTaskQueue(silo)
            .Single()
            .Should().BeOfType<GroundStationRadioMediumGrain.CheckPendingTransmissionsWorkItem>();
    }
    
    // Given: MS#1 and MS#2 tuned;
    //        silence 250ms;
    //        no in-progress conversations;
    //        no pending transmission requests
    //  When: MS#1 registers AI for transmission
    //  Then: task queue has immediate work item to check pending transmissions    
    [Test]
    public void ShortSilence_EnqueueAIForTransmission_ScheduleCheckPendingAfterRemainingSilence()
    {
        //-- given
        
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);

        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        silo.NextWorkItemAtUtc.Should().Be(DateTime.MaxValue);
        SiloTestDoubles.GetWorkItemsInTaskQueue(silo).Should().BeEmpty();
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddMilliseconds(250);
        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyNormal);
        
        //-- then

        var requiredSilence = AirGroundPriority.FlightSafetyNormal.RequiredSilenceBeforeNewConversation(); 
        silo.NextWorkItemAtUtc.Should().Be(environment.UtcNow.Add(requiredSilence).AddMilliseconds(-250));
        
        SiloTestDoubles.GetWorkItemsInTaskQueue(silo)
            .Single()
            .Should().BeOfType<GroundStationRadioMediumGrain.CheckPendingTransmissionsWorkItem>();
    }
    
    // Given: MS#1 and MS#2 tuned;
    //        pending transmissions: MS1/C1(FlightRegularity)
    //        no in-progress conversations;
    //        task queue contains check-pending work item deferred by FlightRegularity required silence;
    //  When: MS#2 registers AI for transmission(FlightSafetyHighest)
    //  Then: check-pending work item updated NotEarlierThan for FlightSafetyHighest required silence;
    [Test]
    public void TaskQueueHasCheckPendingWorkItem_EnqueueAIForTransmission_CheckPendingWorkItemUpdated()
    {
        //-- given
        
        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);

        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightRegularity);

        silo.NextWorkItemAtUtc.Should().Be(
            environment.UtcNow.Add(AirGroundPriority.FlightRegularity.RequiredSilenceBeforeNewConversation())
        );

        SiloTestDoubles.GetWorkItemsInTaskQueue(silo)
            .Single()
            .Should().BeOfType<GroundStationRadioMediumGrain.CheckPendingTransmissionsWorkItem>();

        //-- when

        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain,
            mobiles[0].Operator.Grain, 
            conversationToken: null,
            AirGroundPriority.FlightSafetyHighest);
        
        //-- then

        silo.NextWorkItemAtUtc.Should().Be(
            environment.UtcNow.Add(AirGroundPriority.FlightSafetyHighest.RequiredSilenceBeforeNewConversation())
        );
        
        SiloTestDoubles.GetWorkItemsInTaskQueue(silo)
            .Single()
            .Should().BeOfType<GroundStationRadioMediumGrain.CheckPendingTransmissionsWorkItem>();
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain);
        
        //-- when

        var transmission1 = TestUtility.NewTransmission();
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
            mobiles[0].Station.Grain,
            1
        ), Times.Once);
        
        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            1
        ), Times.Once);

        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>(),
            1
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 registered for transmission;
    //        silence;
    //  When: MS#1 begins to transmit
    //  Then: state: contains transmission and transmitting station id
    //        state: conversation moved from pending to in-progress
    //        notify: NotifyTransmissionStarted invoked on MS#2, GS; 
    [Test]
    public void CanHandleBeginOfTransmissionThatContinuesInProgressConversation()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1,
            new TestIntentA(1));

        //-- when

        var conversationToken1B = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            conversationToken: conversationToken1);

        var transmission2 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken1);
        
        //-- then

        conversationToken1B.Should().BeSameAs(conversationToken1);
        
        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.ConversationsInProgress.Should().BeEquivalentTo(new[] { conversationToken1 });
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.InProgressTransmissionByStationId.Keys.Should().BeEquivalentTo(new[] {
            mobiles[0].Station.Grain.GrainId
        });
        state.IsSilent.Should().BeFalse();
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(20);
        var intent1 = new TestIntentA(1);

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
            It.IsAny<Intent>()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(20);
        var intent1 = new TestIntentA(1, IntentFlags.ConcludesConversation);

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
            It.IsAny<Intent>()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var transmission1 = TestUtility.NewTransmission();
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
            mobiles[0].Station.Grain,
            0
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            0
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>(),
            It.IsAny<int>()
        ), Times.Never);
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting MS1/C1(DirectionFinding);
    //        MS#2 enqueued for transmission MS2/C2(Meteorology);
    //  When: MS#1 completes transmission;
    //  Then: pending check is scheduled after required silence for Meteorology priority  
    [Test]
    public void TransmissionsEnqueued_TransmissionCompletes_PendingCheckScheduled()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.DirectionFinding);
        
        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.Meteorology);

        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(2);
        var intent1 = new TestIntentA(1);

        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1,
            transmittedIntent: intent1);
        
        //-- then

        SiloTestDoubles
            .GetWorkItemsInTaskQueue(silo)
            .OfType<GroundStationRadioMediumGrain.CheckPendingTransmissionsWorkItem>()
            .Count().Should().Be(1);

        silo.NextWorkItemAtUtc.Should().Be(
            environment.UtcNow.Add(AirGroundPriority.Meteorology.RequiredSilenceBeforeNewConversation())
        );
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 transmitting MS1/C1(DirectionFinding);
    //        MS#2 enqueued for transmission MS2/C2(Meteorology);
    //  When: MS#1 completes transmission;
    //  Then: pending check is scheduled after required silence for Meteorology priority  
    [Test]
    public void TransmissionsEnqueued_TransmissionAborted_PendingCheckScheduled()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.DirectionFinding);
        
        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.Meteorology);

        //-- when

        environment.UtcNow = environment.UtcNow.AddSeconds(2);

        mediumGrain.Get().NotifyTransmissionAborted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- then

        SiloTestDoubles
            .GetWorkItemsInTaskQueue(silo)
            .OfType<GroundStationRadioMediumGrain.CheckPendingTransmissionsWorkItem>()
            .Count().Should().Be(1);

        silo.NextWorkItemAtUtc.Should().Be(
            environment.UtcNow.Add(AirGroundPriority.Meteorology.RequiredSilenceBeforeNewConversation())
        );
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- when

        var transmission2 = TestUtility.NewTransmission();
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
            mobiles[0].Station.Grain,
            1
        ), Times.Once);
        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            2
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            2
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            mobiles[0].Station.Grain,
            1
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var transmission2 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);

        //-- when
        
        var intent2 = new TestIntentA(2);
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
            mobiles[1].Station.Grain,
            1
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
            mobiles[1].Station.Grain,
            1
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
            mobiles[1].Station.Grain,
            1
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var transmission2 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2);

        var intent2 = new TestIntentA(2);
        mediumGrain.Get().NotifyTransmissionCompleted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission2,
            conversationToken: conversationToken2,
            transmittedIntent: intent2);

        //-- when
        
        var intent1 = new TestIntentA(1, IntentFlags.ConcludesConversation);
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
            mobiles[0].Station.Grain,
            0
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
            mobiles[0].Station.Grain,
            It.IsAny<int>()
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
            mobiles[0].Station.Grain,
            0
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[0].Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);

        var transmission2 = TestUtility.NewTransmission();
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
            mobiles[1].Station.Grain,
            1
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            1
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.EndReceiveAbortedTransmission(
            transmission2,
            conversationToken2,
            mobiles[1].Station.Grain,
            It.IsAny<int>()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            ground.Station.Grain, 
            ground.Operator.Grain,
            priority: AirGroundPriority.GroundToAir);
        
        //-- when

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: ground.Station.Grain,
            transmission: transmission1,
            conversationToken: conversationToken1);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.IsSilent.Should().BeFalse();
        state.InProgressTransmissionByStationId.Keys.Should().BeEquivalentTo(new[] {
            ground.Station.Grain.GrainId 
        });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] { conversationToken1 });
        state.PendingTransmissionQueue.Should().BeEmpty();
        state.TransmissionWasInterfered.Should().BeFalse();

        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken?>(),
            It.IsAny<GrainRef<IRadioStationGrain>>(),
            1
        ), Times.Never);
        
        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            ground.Station.Grain,
            1
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            ground.Station.Grain,
            1
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var transmission1 = TestUtility.NewTransmission();
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
            mobiles[0].Station.Grain,
            1
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var transmission1 = TestUtility.NewTransmission();
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
            mobiles[0].Station.Grain,
            0
        ), Times.Once);
    }

    // Given: MS#1 and MS#2 is tuned;
    //        MS#2 enqueued pending transmission C1;
    //        MS#1 enqueued pending transmission C2;
    //        MS#2 enqueued pending transmission C3;
    //  When: MS#2 is removed
    //  Then: C1 and C3 and removed from pending transmissions 
    [Test]
    public void CanRemovePendingTransmissionEnqueuedForRemovedStation()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);

        //-- when

        var removedMobile = mobiles[1];
        mediumGrain.Get().RemoveMobileStation(mobiles[1].Station.Grain);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        state.PendingTransmissionQueue.Count.Should().Be(1);
        state.PendingTransmissionQueue.Count(e => e.Token == conversationToken1).Should().Be(0);
        state.PendingTransmissionQueue.Count(e => e.Token == conversationToken2).Should().Be(1);
        state.PendingTransmissionQueue.Count(e => e.Token == conversationToken3).Should().Be(0);
    }

    // Given: MS#1, MS#2, MS#3 tuned;
    //  When: MS#1/C1 enqueued for transmission at priority FlightRegularity 
    //        MS#2/C2 enqueued for transmission at priority Distress
    //        MS#3/C3 enqueued for transmission at priority FlightSafetyNormal
    //  Then: state: pending transmissions order: C2, C3, C1
    [Test]
    public void CanOrderPendingTransmissionsByPriority()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 3, 
            out var ground,
            out var mobiles);

        //-- when

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightRegularity);
        
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.Distress);

        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[2].Station.Grain, 
            mobiles[2].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);

        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(
            new[] {
                conversationToken2,
                conversationToken3,
                conversationToken1
            }, 
            config: options => options.WithStrictOrdering()
        );
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 enqueued for transmission
    //        MS#2 enqueued for transmission
    //  When: MS#1 cancels pending transmission
    //  Then: state: pending transmission is removed from the queue
    [Test]
    public void CanCancelPendingTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[0].Station.Grain, mobiles[0].Operator.Grain);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(mobiles[1].Station.Grain, mobiles[1].Operator.Grain);

        //-- when

        mediumGrain.Get().CancelPendingTransmission(mobiles[0].Station.Grain);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken2
        });
    }

    // Given: MS#1 and MS#2 tuned;
    //        MS#1 enqueued for transmission (non-top-priority)
    //        MS#2 enqueued for transmission (top priority)
    //        MS#3 enqueued for transmission (non-top-priority)
    //  When: CheckPendingTransmissions
    //  Then: state: pending transmissions of MS#1 and MS#3 retained
    //        state: pending transmission of MS#2 removed
    //        MS#2's operator BeginTransmitNow invoked
    [Test]
    public void CanNotifyOperatorToBeginTransmitNow()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 3, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.Meteorology);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.Urgency);
        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[2].Station.Grain, 
            mobiles[2].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);

        //-- when

        mobiles[1].Operator.Mock.Setup(
            x => x.BeginTransmitNow(conversationToken2)
        ).Returns(new BeginTransmitNowResponse(BeganTransmission: true, conversationToken2));
        
        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        mobiles[1].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken2),
            Times.Once);
        mobiles[0].Operator.Mock.Verify(
            x => x.BeginTransmitNow(It.IsAny<ConversationToken>()),
            Times.Never);
        mobiles[2].Operator.Mock.Verify(
            x => x.BeginTransmitNow(It.IsAny<ConversationToken>()),
            Times.Never);
        
        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken1,
            // entry for conversationToken2 was removed 
            conversationToken3
        });
    }

    //--- pending transmission priority queue ---

    //-(1) Ground station? goes first
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //  When: GS/C3 enqueued
    //  Then: pending order: (1) GS/C3, (2) MS1/C1, (3) MS2/C3 
    [Test]
    public void CanPrioritizeGroundStationFirst()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.Urgency);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyHigh);

        //-- when

        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            ground.Station.Grain,
            ground.Operator.Grain,
            priority: AirGroundPriority.GroundToAir);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken3,
            conversationToken1,
            conversationToken2
        }, options => options.WithStrictOrdering());
    }
    
    //-(2) Transmission related to an in-progress conversation? goes first
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C3  
    //  When: MS3/C3 enqueued
    //  Then: pending order: (1) MS3/C3, (2) MS1/C1, (3) MS2/C3 
    [Test]
    public void CanPrioritizeTransmissionRelatedToConversationInProgress()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 3, 
            out var ground,
            out var mobiles);

        var conversationToken1 = new ConversationToken(1);
        var conversationToken2 = new ConversationToken(2);
        var conversationToken3 = new ConversationToken(3);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress
                .Add(conversationToken3),
            PendingTransmissionQueue = mediumState.PendingTransmissionQueue
                .Add(new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                    conversationToken1, mobiles[0].Station.Grain, mobiles[0].Operator.Grain, AirGroundPriority.FlightSafetyHigh))
                .Add(new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                    conversationToken2, mobiles[1].Station.Grain, mobiles[1].Operator.Grain, AirGroundPriority.FlightSafetyHigh))
        });

        //-- when

        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[2].Station.Grain, 
            mobiles[2].Operator.Grain,
            conversationToken3,
            AirGroundPriority.FlightRegularity);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken3, // first because conversationToken3 is in progress
            conversationToken1,
            conversationToken2
        }, options => options.WithStrictOrdering());
    }
    
    //-(5) No duplicate entries
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C1  
    //        C1=FlightSafetyNormal, C2=FlightSafetyNormal  
    //  When: MS2/C2=FlightSafetyNormal enqueued
    //  Then: pending order: (1) MS1/C1, (2) MS2/C2 
    [Test]
    public void PendingTransmissionsCollectionHasNoDuplicates()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = new ConversationToken(1);
        var conversationToken2 = new ConversationToken(2);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress
                .Add(conversationToken1),
            PendingTransmissionQueue = mediumState.PendingTransmissionQueue
                .Add(new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                    conversationToken1, mobiles[0].Station.Grain, mobiles[0].Operator.Grain, AirGroundPriority.FlightSafetyNormal))
                .Add(new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                    conversationToken2, mobiles[1].Station.Grain, mobiles[1].Operator.Grain, AirGroundPriority.FlightSafetyNormal))
        });

        //-- when

        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            conversationToken2,
            AirGroundPriority.FlightSafetyNormal);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken1,
            conversationToken2
        });
    }

    //-(6) Update priority of conversation
    // Given: pending order: (1) MS1/C1, (2) MS2/C2; 
    //        in-progress conversations: C0  
    //        C1=FlightSafetyNormal, C2=FlightSafetyNormal  
    //  When: MS2/C2=Urgency enqueued
    //  Then: pending order: (1) MS2/C2=Urgency, (2) MS1/C1=FlightSafetyNormal 
    [Test]
    public void CanUpdatePendingConversationPriority()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = new ConversationToken(1);
        var conversationToken2 = new ConversationToken(2);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            PendingTransmissionQueue = mediumState.PendingTransmissionQueue
                .Add(new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                    conversationToken1, mobiles[0].Station.Grain, mobiles[0].Operator.Grain, AirGroundPriority.FlightSafetyNormal))
                .Add(new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                    conversationToken2, mobiles[1].Station.Grain, mobiles[1].Operator.Grain, AirGroundPriority.FlightSafetyNormal))
        });

        //-- when

        mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            conversationToken2,
            AirGroundPriority.Urgency);
        
        //-- then

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        var pendingEntries = state.PendingTransmissionQueue.ToArray();

        pendingEntries.Length.Should().Be(2);
        pendingEntries[0].Token.Id.Should().Be(2);
        pendingEntries[0].Priority.Should().Be(AirGroundPriority.Urgency);
        pendingEntries[1].Token.Id.Should().Be(1);
        pendingEntries[1].Priority.Should().Be(AirGroundPriority.FlightSafetyNormal);
    }

    //------- Silence duration rules ---------
    
    // Given: long silence (10 sec)
    //        pending order: (1) MS#1/C1 
    //        in-progress conversations: none  
    //  When: CheckPendingTransmissions
    //  Then: MS#1's operator BeginTransmitNow invoked with C1
    [Test]
    public void LongSilenceNewConversation_CheckPendingTransmissions_BeginTransmitNow()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        mobiles[0].Operator.Mock.Setup(
            x => x.BeginTransmitNow(conversationToken1)
        ).Returns(new BeginTransmitNowResponse(BeganTransmission: true, conversationToken1));

        environment.UtcNow = environment.UtcNow.AddSeconds(10);

        //-- when

        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        mobiles[0].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken1), 
            Times.Once);        
    }

    // Given: short silence (100ms)
    //        pending transmissions: (1) MS#1/C1 
    //        in-progress conversations: none  
    //  When: CheckPendingTransmissions
    //  Then: do nothing
    [Test]
    public void ShortSilenceNewConversation_CheckPendingTransmissions_DoNothing()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        environment.UtcNow = environment.UtcNow.AddMilliseconds(100);

        //-- when

        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        mobiles[0].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken1), 
            Times.Never);        
    }

    // Given: short silence (100ms)
    //        pending transmissions: (1) MS#1/C1 
    //        in-progress conversations: C1  
    //  When: CheckPendingTransmissions
    //  Then: MS#1's operator BeginTransmitNow invoked with C1
    [Test]
    public void ShortSilenceSameConversation_CheckPendingTransmissions_BeginTransmitNow()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress.Add(conversationToken1)
        });

        var conversationToken1B = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            conversationToken: conversationToken1);
        
        environment.UtcNow = environment.UtcNow.AddMilliseconds(100);

        //-- when

        mobiles[0].Operator.Mock.Setup(
            x => x.BeginTransmitNow(conversationToken1)
        ).Returns(new BeginTransmitNowResponse(true, conversationToken1));

        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        mobiles[0].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken1), 
            Times.Once);

        conversationToken1B.Should().BeSameAs(conversationToken1);
    }

    //--- AI operator's response to BeginTransmitNow ---
    
    // Given: pending order: (1) MS1/C1, (2) MS2/C2, (3) MS3/C3 
    //        in-progress conversations: C1, C2, C3  
    //  When: MS#1's BeginTransmitNow(C1) returns BeganTransmission=false;
    //  Then: C1 is removed from in-progress conversations;
    //        MS#2's BeginTransmitNow(C2) is invoked
    //        pending transmissions for C1 and C2 removed
    //        pending transmission for C3 retained
    [Test]
    public void BeginTransmitNow_NotBeganTransmission_RemovedAndNextPendingCalled()
    {
        //-- given

        var environment = new SiloTestDoubles.TestEnvironment {
            UtcNow = new DateTime(2022, 10, 10, 8, 30, 0)
        };
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest, environment: environment);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 3, 
            out var ground,
            out var mobiles);

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        var conversationToken3 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[2].Station.Grain, 
            mobiles[2].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress
                .Add(conversationToken1)
                .Add(conversationToken2)
                .Add(conversationToken3),
        });

        environment.UtcNow = environment.UtcNow.AddMilliseconds(100);

        mobiles[0].Operator.Mock.Setup(
            x => x.BeginTransmitNow(conversationToken1)
        ).Returns(new BeginTransmitNowResponse(BeganTransmission: false));
        mobiles[1].Operator.Mock.Setup(
            x => x.BeginTransmitNow(conversationToken2)
        ).Returns(new BeginTransmitNowResponse(BeganTransmission: true, conversationToken2));

        //-- when

        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        mobiles[0].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken1), 
            Times.Once);
        mobiles[1].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken2), 
            Times.Once);
        mobiles[2].Operator.Mock.Verify(
            x => x.BeginTransmitNow(It.IsAny<ConversationToken>()), 
            Times.Never);

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken3
        });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken2,
            conversationToken3
        });
    }

    // Given: pending order: (1) MS1/C1, (2) MS2/C2 
    //        in-progress conversations: C1, C2  
    //  When: MS#1's BeginTransmitNow(C1) returns new conversation token C3
    //  Then: C3 is added to in-progress conversations
    [Test]
    public void BeginTransmitNow_BeganNewConversation_NewConversationAdded()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress
                .Add(conversationToken1)
                .Add(conversationToken2)
        });

        environment.UtcNow = environment.UtcNow.AddMilliseconds(100);

        var transmission3 = TestUtility.NewTransmission();
        var conversationToken3 = mediumGrain.Get().TakeNewAIConversationToken(); 
        
        mobiles[0].Operator.Mock.Setup(x => x.BeginTransmitNow(conversationToken1))
            .Callback(() => {
                mediumGrain.Get().NotifyTransmissionStarted(mobiles[0].Station.Grain, transmission3, conversationToken3);
            })
            .Returns(new BeginTransmitNowResponse(BeganTransmission: true, ConversationToken: conversationToken3));

        //-- when

        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        Console.WriteLine($"{conversationToken1.Id}, {conversationToken2.Id}, {conversationToken3!.Id}");

        mobiles[0].Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken1), 
            Times.Once);

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken2
        });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1,
            conversationToken2,
            conversationToken3
        });
    }

    // Given: pending order: (0) GND/C0, (1) MS1/C1, (2) MS2/C2 
    //        in-progress conversations: C1  
    //  When: GND's BeginTransmitNow(C0) returns conversation token C2
    //  Then: state: in-progress conversations: C1, C2
    //        state: pending transmissions: (1) MS1/C1, (2) MS2/C2
    [Test]
    public void BeginTransmitNow_SwitchConversation_BothConversationsInProgress()
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

        var conversationToken0 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            ground.Station.Grain, 
            ground.Operator.Grain,
            priority: AirGroundPriority.GroundToAir);
        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        var conversationToken2 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress.Add(conversationToken1)
        });

        environment.UtcNow = environment.UtcNow.AddMilliseconds(100);

        var transmission0 = TestUtility.NewTransmission();
        
        ground.Operator.Mock.Setup(x => x.BeginTransmitNow(conversationToken0))
            .Callback(() => {
                mediumGrain.Get().NotifyTransmissionStarted(ground.Station.Grain, transmission0, conversationToken2);
            })
            .Returns(new BeginTransmitNowResponse(BeganTransmission: true, ConversationToken: conversationToken2));

        //-- when

        mediumGrain.Get().CheckPendingTransmissions();
        
        //-- then

        ground.Operator.Mock.Verify(
            x => x.BeginTransmitNow(conversationToken0), 
            Times.Once);

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Should().BeEquivalentTo(new[] {
            new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                conversationToken1, mobiles[0].Station.Grain, mobiles[0].Operator.Grain, AirGroundPriority.FlightSafetyNormal),
            // the MS2/C2 entry retained, even though it's related to C2
            new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
                conversationToken2, mobiles[1].Station.Grain, mobiles[1].Operator.Grain, AirGroundPriority.FlightSafetyNormal),
        });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1,
            conversationToken2
        });
    }

    //--- Support for transmissions by human ---
    
    // Given: MS1 - station operated by AI, MS2 - station operated by human
    //        pending transmissions: (1) MS1/C1 
    //        in-progress conversations: C1
    //        MS2 has not called EnqueueAIOperatorForTransmission
    //  When: MS2 invokes NotifyTransmissionStarted without ConversationToken
    //  Then: state: contains transmission and transmitting station id
    //        state: pending transmissions not changed
    //        state: pending conversations not changed
    //        notify: NotifyTransmissionStarted invoked on MS#1, MS#2, GS; 
    [Test]
    public void CanHandleTransmissionsByHumans()
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

        var conversationToken1 = mediumGrain.Get().EnqueueAIOperatorForTransmission(
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress
                .Add(conversationToken1)
        });

        environment.UtcNow = environment.UtcNow.AddMilliseconds(100);

        //-- when

        var transmission1 = TestUtility.NewTransmission();
        mediumGrain.Get().NotifyTransmissionStarted(
            stationTransmitting: mobiles[1].Station.Grain,
            transmission: transmission1,
            conversationToken: null);
        
        //-- then

        ground.Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            null,
            mobiles[1].Station.Grain,
            1
        ), Times.Once);
        
        mobiles[0].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            transmission1,
            null,
            mobiles[1].Station.Grain,
            1
        ), Times.Once);

        mobiles[1].Station.Mock.Verify(x => x.BeginReceiveTransmission(
            It.IsAny<TransmissionDescription>(),
            It.IsAny<ConversationToken>(),
            It.IsAny<GrainRef<IRadioStationGrain>>(),
            1
        ), Times.Never);

        var state = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        state.PendingTransmissionQueue.Select(entry => entry.Token).Should().BeEquivalentTo(new[] {
            conversationToken1
        });
        state.ConversationsInProgress.Should().BeEquivalentTo(new[] {
            conversationToken1
        });
    }

    [Test]
    public void Debug_RemovePendingTransmissionQueueEntry()
    {
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var mediumGrain = SetupGroundStationRadioMediumGrain(
            silo, 
            mobileStationCount: 2, 
            out var ground,
            out var mobiles);

        var conversationToken1 = new ConversationToken(1);
        var conversationToken2 = new ConversationToken(2);
        var conversationToken3 = new ConversationToken(3);

        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            ConversationsInProgress = mediumState.ConversationsInProgress
                .Add(conversationToken2)
                .Add(conversationToken3)
        });

        var entry1 = new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
            conversationToken1,
            ground.Station.Grain, 
            ground.Operator.Grain,
            AirGroundPriority.GroundToAir); 
        var entry2 = new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
            conversationToken2,
            mobiles[0].Station.Grain, 
            mobiles[0].Operator.Grain,
            AirGroundPriority.FlightSafetyNormal); 
        var entry3 = new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
            conversationToken3,
            mobiles[1].Station.Grain, 
            mobiles[1].Operator.Grain,
            AirGroundPriority.FlightSafetyNormal);

        var pendingTransmissionComparer =
            new GroundStationRadioMediumGrain.PendingTransmissionQueueEntryComparer(mediumGrain.Get());

        var set0 = ImmutableSortedSet.Create<GroundStationRadioMediumGrain.PendingTransmissionQueueEntry>(pendingTransmissionComparer)
            .Add(entry1)
            .Add(entry2)
            .Add(entry3);

        // var entryToRemove = new GroundStationRadioMediumGrain.PendingTransmissionQueueEntry(
        //     new ConversationToken(1, AirGroundPriority.GroundToAir),
        //     ground.Station.Grain, 
        //     ground.Operator.Grain);

        set0.First().Token.Id.Should().Be(1);
        
        var set1 = set0.Remove(set0.First());
        set1.Count.Should().Be(2);
    }

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
        Location location = default)
    {
        var radioStation = TestUtility.MockGrain<IRadioStationGrain>();

        radioStation.Mock.SetupGet(x => x.StationType).Returns(RadioStationType.Ground);
        radioStation.Mock.SetupGet(x => x.Location).Returns(location);
        radioStation.Mock.SetupGet(x => x.Frequency).Returns(frequency);

        return radioStation;
    }

    private MockedGrain<IRadioStationGrain> MockMobileStation(Frequency frequency = default)
    {
        var radioStation = TestUtility.MockGrain<IRadioStationGrain>();

        radioStation.Mock.SetupGet(x => x.StationType).Returns(RadioStationType.Mobile);
        radioStation.Mock.SetupGet(x => x.Location).Returns(default(Location));
        radioStation.Mock.SetupGet(x => x.Frequency).Returns(frequency);

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
    //             Transmission: transmission ?? TestUtility.NewTransmission(),
    //             ConversationToken: token);
    //     }
    // }
}
