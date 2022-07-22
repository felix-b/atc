using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Atc.World.Tests.Communications;

[TestFixture]
public class RadioStationGrainTests
{
    [Test]
    public void CanInitializeGroundStation()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId => 
            new RadioStationGrain.GrainActivationEvent(
                grainId, 
                RadioStationType.Ground));

        //-- when 
        
        stationGrain.Get().TurnOnGroundStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        var mediumGrain = state.GroundStationMedium!.Value; 

        world.Mock.Verify(x => x.AddRadioMedium(mediumGrain));

        // stationGrain.Get().StationType.Should().Be(RadioStationType.Ground);
        // stationGrain.Get().Frequency.Should().Be(Frequency.FromKhz(118000));
        // stationGrain.Get().Location.Should().Be(GeoPoint.LatLon(10f, 20f));
        // stationGrain.Get().TransceiverState.Should().NotBeNull();
        // stationGrain.Get().TransceiverState.Status.Should().Be(TransceiverStatus.Silence);
        // stationGrain.Get().TransceiverState.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        // stationGrain.Get().TransceiverState.PttPressed.Should().BeFalse();
        // stationGrain.Get().TransceiverState.CurrentTransmission.Should().BeNull();
        // stationGrain.Get().TransceiverState.ConversationToken.Should().BeNull();

        state.StationType.Should().Be(RadioStationType.Ground);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.Silence);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.Listeners.Should().BeEquivalentTo(new[] {
            new RadioStationGrain.RadioStationListenerEntry(mediumGrain.As<IRadioStationListener>(), RadioStationListenerMask.Transmitter)            
        });
    }

    [Test]
    public void CanTurnOnMobileStationWithoutAvailableMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId => 
            new RadioStationGrain.GrainActivationEvent(
                grainId, 
                RadioStationType.Mobile));

        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f), 
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);
        
        //-- when 
        
        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.NoMedium);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.GroundStationMedium.HasValue.Should().BeFalse();
        state.Listeners.Should().BeEmpty();

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        ), Times.Once);
    }

    [Test]
    public void CanTurnOnMobileStationWithAvailableMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId => 
            new RadioStationGrain.GrainActivationEvent(
                grainId, 
                RadioStationType.Mobile));

        var mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(
            grainId => new GroundStationRadioMediumGrain.GrainActivationEvent(grainId));
        
        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f), 
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns(mediumGrain.As<IGroundStationRadioMediumGrain>());
        
        //-- when 
        
        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.Silence);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.GroundStationMedium!.Value.Should().Be(mediumGrain);
        state.Listeners.Should().BeEquivalentTo(new[] {
            new RadioStationGrain.RadioStationListenerEntry(mediumGrain.As<IRadioStationListener>(), RadioStationListenerMask.Transmitter)            
        });

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        ), Times.Once);
    }

    [Test]
    public void CanTurnOnMobileStationWithAvailableMediumAndOngoingTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);

        SetupInProgressTransmission(mediumGrain, out var conversationToken1, out var transmission1);

        //-- when 
        
        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        state.CurrentTransmission.Should().BeSameAs(transmission1);
        state.ConversationToken.Should().BeSameAs(conversationToken1);
        state.GroundStationMedium!.Value.Should().Be(mediumGrain);
        state.Listeners.Should().BeEquivalentTo(new[] {
            new RadioStationGrain.RadioStationListenerEntry(mediumGrain.As<IRadioStationListener>(), RadioStationListenerMask.Transmitter)            
        });

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        ), Times.Once);
    }

    [Test]
    public void CanTurnOffMobileStationWithoutMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId => 
            new RadioStationGrain.GrainActivationEvent(
                grainId, 
                RadioStationType.Mobile));

        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f), 
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);
        
        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- when 
        
        stationGrain.Get().TurnOffMobileStation();

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.Off);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.GroundStationMedium.HasValue.Should().BeFalse();
        state.Listeners.Should().BeEmpty();
    }
    
    [Test]
    public void CanTurnOffMobileStationWithMediumAndSilence()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);

        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- when 
        
        stationGrain.Get().TurnOffMobileStation();

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.Off);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.GroundStationMedium.HasValue.Should().BeFalse();
        state.Listeners.Should().BeEmpty();

        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        mediumState.MobileStationById.Should().BeEmpty();
    }
    
    [Test]
    public void CanTurnOffMobileStationWhileReceivingTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);

        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Receiver);

        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        var (transmission1, conversationToken1) = BeginTransmissionByGroundStation(
            mediumGrain, 
            out var groundStationGrain);
        SiloTestDoubles.GetGrainState(stationGrain.Get()).Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        
        //-- when 
        
        stationGrain.Get().TurnOffMobileStation();

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.Off);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.GroundStationMedium.HasValue.Should().BeFalse();
        state.Listeners.Should().BeEmpty();

        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        mediumState.MobileStationById.Should().BeEmpty();
        
        listener.Mock.Verify(x => x.NotifyTransmissionAborted(
            groundStationGrain.As<IRadioStationGrain>(),
            transmission1,
            conversationToken1
        ), Times.Once);
    }

    [Test]
    public void CanTuneMobileStationWithMediumToNewFrequencyWithoutMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);

        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);
        
        //-- when 
        
        stationGrain.Get().TuneMobileStation(        
            Location.At(lat: 11f, lon: 21f, elevationFt: 200f), 
            Frequency.FromKhz(119000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(119000));
        state.LastKnownLocation.Should().Be(Location.At(11f, 21f, 200f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.NoMedium);
        state.CurrentTransmission.Should().BeNull();
        state.ConversationToken.Should().BeNull();
        state.GroundStationMedium.HasValue.Should().BeFalse();
        state.Listeners.Should().BeEmpty();

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        ), Times.Once);
    }

    [Test]
    public void CanTuneMobileStationWithoutMediumToNewFrequencyWithMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        SetupInProgressTransmission(mediumGrain, out var conversationToken1, out var transmission1);
        
        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);

        stationGrain.Get().TurnOnMobileStation(
            Location.At(lat: 11f, lon: 21f, elevationFt: 200f), 
            Frequency.FromKhz(119000));

        //-- when 
        
        stationGrain.Get().TuneMobileStation(        
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        state.CurrentTransmission.Should().BeSameAs(transmission1);
        state.ConversationToken.Should().BeSameAs(conversationToken1);
        state.GroundStationMedium!.Value.Should().Be(mediumGrain);
        state.Listeners.Should().BeEquivalentTo(new[] {
            new RadioStationGrain.RadioStationListenerEntry(
                Listener: mediumGrain.As<IRadioStationListener>(), 
                RadioStationListenerMask.Transmitter)
        });

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        ), Times.Once);
        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        ), Times.Once);
    }

    [Test]
    public void CanTuneMobileStationFromOneMediumToAnother()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        
        var mediumGrain1 = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(
            grainId => new GroundStationRadioMediumGrain.GrainActivationEvent(grainId));
        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns(mediumGrain1.As<IGroundStationRadioMediumGrain>());

        var mediumGrain2 = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(
            grainId => new GroundStationRadioMediumGrain.GrainActivationEvent(grainId));
        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        )).Returns(mediumGrain2.As<IGroundStationRadioMediumGrain>());

        SetupInProgressTransmission(mediumGrain2, out var conversationToken1, out var transmission1);

        var mobileStationGrain = silo.Grains.CreateGrain<RadioStationGrain>(
            grainId => new RadioStationGrain.GrainActivationEvent(grainId, RadioStationType.Mobile));
        
        mobileStationGrain.Get().TurnOnMobileStation(        
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- when 
        
        mobileStationGrain.Get().TuneMobileStation(        
            Location.At(lat: 11f, lon: 21f, elevationFt: 200f), 
            Frequency.FromKhz(119000));

        //-- then

        var mobileState = SiloTestDoubles.GetGrainState(mobileStationGrain.Get());
        var medium1State = SiloTestDoubles.GetGrainState(mediumGrain1.Get());
        var medium2State = SiloTestDoubles.GetGrainState(mediumGrain2.Get());

        medium1State.MobileStationById.ContainsKey(mobileStationGrain.GrainId).Should().BeFalse();
        medium2State.MobileStationById.ContainsKey(mobileStationGrain.GrainId).Should().BeTrue();
        
        mobileState.StationType.Should().Be(RadioStationType.Mobile);
        mobileState.SelectedFrequency.Should().Be(Frequency.FromKhz(119000));
        mobileState.LastKnownLocation.Should().Be(Location.At(11f, 21f, 200f));
        mobileState.PttPressed.Should().BeFalse();
        mobileState.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        mobileState.CurrentTransmission.Should().BeSameAs(transmission1);
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);
        mobileState.GroundStationMedium!.Value.Should().Be(mediumGrain2);
        mobileState.Listeners.Should().BeEquivalentTo(new[] {
            new RadioStationGrain.RadioStationListenerEntry(
                Listener: mediumGrain2.As<IRadioStationListener>(), 
                RadioStationListenerMask.Transmitter)
        });

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        ), Times.Once);
        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        ), Times.Once);
    }

    [Test]
    public void CanTuneMobileStationWithTransmissionsInProgressInBothMediums()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var groundStation1Grain = CreateGroundStation(
            silo,
            world,
            Location.At(10f, 20f, 100f),
            Frequency.FromKhz(118000),
            out var medium1Grain); 

        var groundStation2Grain = CreateGroundStation(
            silo,
            world,
            Location.At(11f, 21f, 200f),
            Frequency.FromKhz(119000),
            out var medium2Grain);

        var mobileStationGrain = CreateMobileStationTunedToGround(silo, medium1Grain, turnedOn: true);
        var listener = AddMockedListener(mobileStationGrain, RadioStationListenerMask.Receiver);

        var (transmission1, conversationToken1) = BeginTransmissionByGroundStation(medium1Grain, out _);
        var (transmission2, conversationToken2) = BeginTransmissionByGroundStation(medium2Grain, out _);

        mobileStationGrain.Get().TransceiverState.Status.Should().Be(
            TransceiverStatus.ReceivingSingleTransmission
        );

        //-- when 
        
        mobileStationGrain.Get().TuneMobileStation(        
            Location.At(lat: 11f, lon: 21f, elevationFt: 200f), 
            Frequency.FromKhz(119000));

        //-- then

        var mobileState = SiloTestDoubles.GetGrainState(mobileStationGrain.Get());
        var medium1State = SiloTestDoubles.GetGrainState(medium1Grain.Get());
        var medium2State = SiloTestDoubles.GetGrainState(medium2Grain.Get());

        medium1State.MobileStationById.ContainsKey(mobileStationGrain.GrainId).Should().BeFalse();
        medium2State.MobileStationById.ContainsKey(mobileStationGrain.GrainId).Should().BeTrue();

        mobileState.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        
        listener.Mock.Verify(x => x.NotifyTransmissionStarted(
            groundStation1Grain.As<IRadioStationGrain>(),
            transmission1,
            conversationToken1
        ), Times.Once);

        listener.Mock.Verify(x => x.NotifyTransmissionAborted(
            groundStation1Grain.As<IRadioStationGrain>(),
            transmission1,
            conversationToken1
        ), Times.Once);
        
        listener.Mock.Verify(x => x.NotifyTransmissionStarted(
            groundStation2Grain.As<IRadioStationGrain>(),
            transmission2,
            conversationToken2
        ), Times.Once);
    }
    
    [Test]
    public void CanTuneMobileStationWithNoMediumBeforeAndAfter()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        
        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);

        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(119000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);

        var mobileStationGrain = silo.Grains.CreateGrain<RadioStationGrain>(
            grainId => new RadioStationGrain.GrainActivationEvent(grainId, RadioStationType.Mobile));
        
        mobileStationGrain.Get().TurnOnMobileStation(        
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        //-- when 
        
        mobileStationGrain.Get().TuneMobileStation(        
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(119000));

        //-- then

        var mobileState = SiloTestDoubles.GetGrainState(mobileStationGrain.Get());
        mobileState.StationType.Should().Be(RadioStationType.Mobile);
        mobileState.SelectedFrequency.Should().Be(Frequency.FromKhz(119000));
        mobileState.LastKnownLocation.Should().Be(Location.At(10f, 20f, 100f));
        mobileState.PttPressed.Should().BeFalse();
        mobileState.Status.Should().Be(TransceiverStatus.NoMedium);
        mobileState.CurrentTransmission.Should().BeNull();
        mobileState.ConversationToken.Should().BeNull();
        mobileState.GroundStationMedium.HasValue.Should().BeFalse();
        mobileState.Listeners.Should().BeEmpty();

        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        ), Times.Once);
        world.Mock.Verify(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(119000)
        ), Times.Once);
    }

    [Test]
    public void CannotTuneMobileStationIfPoweredOff()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        
        CreateGroundStation(
            silo, world, Location.At(10f, 20f, 100f), Frequency.FromKhz(118000), out var mediumGrain);

        var mobileStationGrain = CreateMobileStationTunedToGround(silo, mediumGrain, turnedOn: false);
        
        //-- then

        Assert.Throws<InvalidOperationException>(() => {
            mobileStationGrain.Get().TuneMobileStation(
                Location.At(10f, 20f, 100f),
                Frequency.FromKhz(119000));
        });
    }
    
    [Test]
    public void CanBeginReceiveTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);
        
        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Receiver);

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();
        var stationTransmitting = mediumGrain.Get().GroundStation; 

        //-- when
        
        stationGrain.Get().BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            stationTransmitting,
            transmittingStationsCount: 1);
        
        //-- then
        
        var mobileState = SiloTestDoubles.GetGrainState(stationGrain.Get());
        mobileState.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        mobileState.CurrentTransmission.Should().BeSameAs(transmission1);
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);

        listener.Mock.Verify(x => x.NotifyTransmissionStarted(
            stationTransmitting,
            transmission1,
            conversationToken1
        ), Times.Once);
    }

    [Test]
    public void CanEndReceiveCompletedTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Receiver);

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();
        var intent1 = new TestIntentA(1);
        var stationTransmitting = mediumGrain.Get().GroundStation; 

        stationGrain.Get().BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            stationTransmitting,
            transmittingStationsCount: 1);
        
        //-- when
        
        stationGrain.Get().EndReceiveCompletedTransmission(
            transmission1, 
            conversationToken1, 
            stationTransmitting,
            intent1);
        
        //-- then
        
        var mobileState = SiloTestDoubles.GetGrainState(stationGrain.Get());
        mobileState.Status.Should().Be(TransceiverStatus.Silence);
        mobileState.CurrentTransmission.Should().BeNull();
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);

        listener.Mock.Verify(x => x.NotifyTransmissionCompleted(
            stationTransmitting,
            transmission1,
            conversationToken1,
            intent1
        ), Times.Once);
    }

    [Test]
    public void CanEndReceiveAbortedTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Receiver);

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();
        var stationTransmitting = mediumGrain.Get().GroundStation; 

        stationGrain.Get().BeginReceiveTransmission(
            transmission1,
            conversationToken1,
            stationTransmitting,
            transmittingStationsCount: 1);
        
        //-- when
        
        stationGrain.Get().EndReceiveAbortedTransmission(
            transmission1, 
            conversationToken1, 
            stationTransmitting,
            transmittingStationsCount: 0);
        
        //-- then
        
        var mobileState = SiloTestDoubles.GetGrainState(stationGrain.Get());
        mobileState.Status.Should().Be(TransceiverStatus.Silence);
        mobileState.CurrentTransmission.Should().BeNull();
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);

        listener.Mock.Verify(x => x.NotifyTransmissionAborted(
            stationTransmitting,
            transmission1,
            conversationToken1
        ), Times.Once);
    }

    [Test]
    public void CannotBeginReceiveTransmissionIfPoweredOff()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        
        var groundStationGrain = CreateGroundStation(
            silo, world, Location.At(10f, 20f, 100f), Frequency.FromKhz(118000), out var mediumGrain);

        var mobileStationGrain = CreateMobileStationTunedToGround(silo, mediumGrain, turnedOn: false);
        
        //-- then

        Assert.Throws<InvalidOperationException>(() => {
            mobileStationGrain.Get().BeginReceiveTransmission(
                TestUtility.NewTransmission(),
                new ConversationToken(1),
                groundStationGrain.As<IRadioStationGrain>(),
                transmittingStationsCount: 1);
        });
    }

    [Test]
    public void CanHandleStartOfInterferingTransmission()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var aiOperator1 = TestUtility.MockGrain<IAIRadioOperatorGrain>();
        var aiOperator2 = TestUtility.MockGrain<IAIRadioOperatorGrain>();
        
        var groundStation = CreateGroundStation(
            silo, world, Location.At(10f, 20f, 100f), Frequency.FromKhz(118000), out var mediumGrain);

        var mobileStation1Grain = CreateMobileStationTunedToGround(silo, mediumGrain, turnedOn: true);
        var mobileStation2Grain = CreateMobileStationTunedToGround(silo, mediumGrain, turnedOn: true);

        var conversationToken1 = mobileStation1Grain.Get().EnqueueAIOperatorForTransmission(
            aiOperator1.Grain, 
            AirGroundPriority.FlightSafetyNormal);
        var transmission1 = TestUtility.NewTransmission();
        mobileStation1Grain.Get().BeginTransmission(transmission1, conversationToken1);

        var conversationToken2 = mobileStation1Grain.Get().EnqueueAIOperatorForTransmission(
            aiOperator2.Grain, 
            AirGroundPriority.FlightSafetyNormal);
        var transmission2 = TestUtility.NewTransmission();
        
        mobileStation1Grain.Get().TransceiverState.Status.Should().Be(TransceiverStatus.Transmitting);
        mobileStation2Grain.Get().TransceiverState.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        groundStation.Get().TransceiverState.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);

        //-- when

        mobileStation2Grain.Get().BeginTransmission(transmission2, conversationToken2);
        
        //-- then

        mobileStation1Grain.Get().TransceiverState.Status.Should().Be(TransceiverStatus.Transmitting);
        mobileStation2Grain.Get().TransceiverState.Status.Should().Be(TransceiverStatus.Transmitting);
        groundStation.Get().TransceiverState.Status.Should().Be(TransceiverStatus.ReceivingInterferenceNoise);
    }

    [Test]
    public void CanEnqueueAIOperatorForTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var aiOperator = TestUtility.MockGrain<IAIRadioOperatorGrain>();
        
        //-- when
        
        var conversationToken = stationGrain.Get().EnqueueAIOperatorForTransmission(
            aiOperator.Grain,
            AirGroundPriority.FlightSafetyNormal);
        
        //-- then

        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        var queueEntry = mediumState.PendingTransmissionQueue.Single();
        queueEntry.Token.Should().BeSameAs(conversationToken);
        queueEntry.Priority.Should().Be(AirGroundPriority.FlightSafetyNormal);
        queueEntry.Operator.Should().Be(aiOperator.Grain);
        queueEntry.Station.Should().Be(stationGrain.As<IRadioStationGrain>());
    }

    [Test]
    public void CanEnqueueAIOperatorForTransmissionAndUpdateExistingPriority()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var aiOperator = TestUtility.MockGrain<IAIRadioOperatorGrain>();
        var conversationToken1 = stationGrain.Get().EnqueueAIOperatorForTransmission(
            aiOperator.Grain,
            AirGroundPriority.FlightSafetyNormal);
        
        //-- when
        
        var conversationToken2 = stationGrain.Get().EnqueueAIOperatorForTransmission(
            aiOperator.Grain,
            AirGroundPriority.Urgency,
            conversationToken: conversationToken1);
        
        //-- then

        conversationToken2.Id.Should().Be(conversationToken1.Id);
        
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());
        var queueEntry = mediumState.PendingTransmissionQueue.Single();
        queueEntry.Token.Should().BeSameAs(conversationToken2);
        queueEntry.Priority.Should().Be(AirGroundPriority.Urgency);
    }

    [Test]
    public void CannotEnqueueAIOperatorForTransmissionIfStationPoweredOff()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var aiOperator = TestUtility.MockGrain<IAIRadioOperatorGrain>();
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);

        //-- then

        Assert.Throws<InvalidOperationException>(() => {
            stationGrain.Get().EnqueueAIOperatorForTransmission(
                aiOperator.Grain,
                AirGroundPriority.FlightSafetyNormal);
        });
    }

    [Test]
    public void CannotEnqueueAIOperatorForTransmissionIfNotTunedToMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var aiOperator = TestUtility.MockGrain<IAIRadioOperatorGrain>();

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, Frequency.FromKhz(133000));
        
        //-- then

        Assert.Throws<InvalidOperationException>(() => {
            stationGrain.Get().EnqueueAIOperatorForTransmission(
                aiOperator.Grain,
                AirGroundPriority.FlightSafetyNormal);
        });
    }

    [Test]
    public void CanBeginTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Transmitter);

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();

        //-- when
        
        stationGrain.Get().BeginTransmission(transmission1, conversationToken1);
        
        //-- then
        
        var mobileState = SiloTestDoubles.GetGrainState(stationGrain.Get());
        mobileState.Status.Should().Be(TransceiverStatus.Transmitting);
        mobileState.PttPressed.Should().BeTrue();
        mobileState.CurrentTransmission.Should().BeSameAs(transmission1);
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);

        listener.Mock.Verify(x => x.NotifyTransmissionStarted(
            stationGrain.As<IRadioStationGrain>(),
            transmission1,
            conversationToken1
        ), Times.Once);
    }

    [Test]
    public void CanCompleteTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Transmitter);

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();
        var intent1 = new TestIntentA(1);

        stationGrain.Get().BeginTransmission(transmission1, conversationToken1);

        //-- when
        
        stationGrain.Get().CompleteTransmission(intent1, keepPttPressed: false);
        
        //-- then
        
        var mobileState = SiloTestDoubles.GetGrainState(stationGrain.Get());
        mobileState.Status.Should().Be(TransceiverStatus.Silence);
        mobileState.PttPressed.Should().BeFalse();
        mobileState.CurrentTransmission.Should().BeNull();
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);

        listener.Mock.Verify(x => x.NotifyTransmissionCompleted(
            stationGrain.As<IRadioStationGrain>(),
            transmission1,
            conversationToken1,
            intent1
        ), Times.Once);
    }

    [Test]
    public void CanAbortTransmission()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, RadioStationListenerMask.Transmitter);

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();
        var intent1 = new TestIntentA(1);

        stationGrain.Get().BeginTransmission(transmission1, conversationToken1);

        //-- when
        
        stationGrain.Get().AbortTransmission();
        
        //-- then
        
        var mobileState = SiloTestDoubles.GetGrainState(stationGrain.Get());
        mobileState.Status.Should().Be(TransceiverStatus.Silence);
        mobileState.PttPressed.Should().BeFalse();
        mobileState.CurrentTransmission.Should().BeNull();
        mobileState.ConversationToken.Should().BeSameAs(conversationToken1);

        listener.Mock.Verify(x => x.NotifyTransmissionAborted(
            stationGrain.As<IRadioStationGrain>(),
            transmission1,
            conversationToken1
        ), Times.Once);
    }

    [Test]
    public void CannotBeginTransmissionIfPoweredOff()
    {
        //-- given
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        
        //-- when

        stationGrain.Get().TransceiverState.Status.Should().Be(TransceiverStatus.Off);

        //-- then 
        
        Assert.Throws<InvalidOperationException>(() => {
            stationGrain.Get().BeginTransmission(
                TestUtility.NewTransmission(),
                new ConversationToken(1));
        });
    }

    [Test]
    public void CanRaiseCSharpEventOnStateChanged()
    {
        List<ITransceiverState> stateLog = new();
        
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);
        stationGrain.Get().OnTransceiverStateChanged += stateLog.Add; 
        stationGrain.Get().TurnOnMobileStation(mediumGrain.Get().AntennaLocation, mediumGrain.Get().Frequency);

        //-- when

        var conversationToken1 = new ConversationToken(1);
        var transmission1 = TestUtility.NewTransmission();
        var intent1 = new TestIntentA(1);

        stationGrain.Get().BeginTransmission(transmission1, conversationToken1);
        stationGrain.Get().CompleteTransmission(intent1, keepPttPressed: false);
        
        //-- then

        stateLog.Count.Should().Be(3);
        
        stateLog[0].Status.Should().Be(TransceiverStatus.Silence);
        stateLog[0].CurrentTransmission.Should().BeNull();
        stateLog[0].ConversationToken.Should().BeNull();
        
        stateLog[1].Status.Should().Be(TransceiverStatus.Transmitting);
        stateLog[1].CurrentTransmission.Should().BeSameAs(transmission1);
        stateLog[1].ConversationToken.Should().BeSameAs(conversationToken1);
        
        stateLog[2].Status.Should().Be(TransceiverStatus.Silence);
        stateLog[2].CurrentTransmission.Should().BeNull();
        stateLog[2].ConversationToken.Should().BeSameAs(conversationToken1);
    }

    [Test]
    public void CanRaiseCSharpEventOnIntentCaptured()
    {
        List<Intent> intentLog = new();
        
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);

        var groundStation = CreateGroundStation(
            silo, 
            world, 
            Location.At(10f, 20f, 100f), 
            Frequency.FromKhz(118000), 
            out var mediumGrain);

        var mobileStation1Grain = CreateMobileStationTunedToGround(
            silo, 
            mediumGrain, 
            turnedOn: true);

        groundStation.Get().OnIntentCaptured += intentLog.Add;

        //-- when

        var transmission1 = TestUtility.NewTransmission();
        var intent1 = new TestIntentA(1);
            
        groundStation.Get().BeginTransmission(transmission1, conversationToken: null);
        groundStation.Get().CompleteTransmission(intent1);

        var transmission2 = TestUtility.NewTransmission();
        var intent2 = new TestIntentA(2);

        mobileStation1Grain.Get().BeginTransmission(transmission2, conversationToken: null);
        mobileStation1Grain.Get().CompleteTransmission(intent2);
        
        //-- then

        intentLog.Count.Should().Be(2);
        intentLog[0].Should().BeSameAs(intent1);
        intentLog[1].Should().BeSameAs(intent2);
    }

    private GrainRef<RadioStationGrain> CreateGroundStation(
        ISilo silo,
        MockedGrain<IWorldGrain> world,
        Location location,
        Frequency frequency,
        out GrainRef<GroundStationRadioMediumGrain> mediumGrain)
    {
        var groundStationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Ground));

        groundStationGrain.Get().TurnOnGroundStation(location, frequency);
        mediumGrain = groundStationGrain.Get().GroundStationMedium!.Value.As<GroundStationRadioMediumGrain>();
        
        world.Mock.Setup(x => x.TryFindRadioMedium(
            location.Position,
            location.Altitude,
            frequency
        )).Returns(mediumGrain.As<IGroundStationRadioMediumGrain>());

        return groundStationGrain;
    }

    private GrainRef<RadioStationGrain> CreateMobileStationTunedToGround(
        ISilo silo,
        GrainRef<GroundStationRadioMediumGrain> groundMediumGrain,
        bool turnedOn = true)
    {
        var mobileStationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Mobile));

        if (turnedOn)
        {
            mobileStationGrain.Get().TurnOnMobileStation(
                groundMediumGrain.Get().AntennaLocation, 
                groundMediumGrain.Get().Frequency); 
        }

        return mobileStationGrain;
    }

    private MockedGrain<IRadioStationListener> AddMockedListener(
        GrainRef<RadioStationGrain> stationGrain, 
        RadioStationListenerMask mask)
    {
        var listener = TestUtility.MockGrain<IRadioStationListener>();
        stationGrain.Get().AddListener(listener.Grain, mask);
        return listener;
    }

    private GrainRef<RadioStationGrain> CreateMobileStationWithMediumSetup(
        ISilo silo, 
        MockedGrain<IWorldGrain> world, 
        out GrainRef<GroundStationRadioMediumGrain> mediumGrain)
    {
        var groundStationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Ground));

        groundStationGrain.Get().TurnOnGroundStation(
            Location.At(lat: 10f, lon: 20f, elevationFt: 100f), 
            Frequency.FromKhz(118000));

        mediumGrain = groundStationGrain.Get().GroundStationMedium!.Value.As<GroundStationRadioMediumGrain>();
        
        var mobileStationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Mobile));
        
        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns(mediumGrain.As<IGroundStationRadioMediumGrain>());

        return mobileStationGrain;
    }

    private void SetupInProgressTransmission(
        GrainRef<GroundStationRadioMediumGrain> mediumGrain, 
        out ConversationToken conversationToken,
        out TransmissionDescription transmission)
    {
        conversationToken = new ConversationToken(1);
        transmission = TestUtility.NewTransmission();

        var otherMobileStation = TestUtility.MockGrain<IRadioStationGrain>();
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            InProgressTransmissionByStationId = ImmutableDictionary<string, GroundStationRadioMediumGrain.InProgressTransmissionEntry>
                .Empty
                .Add(otherMobileStation.Grain.GrainId, new GroundStationRadioMediumGrain.InProgressTransmissionEntry(
                    otherMobileStation.Grain,
                    conversationToken,
                    transmission
                ))
        });
    }

    private static (TransmissionDescription transmission, ConversationToken token) BeginTransmissionByGroundStation(
        GrainRef<GroundStationRadioMediumGrain> mediumGrain,
        out GrainRef<RadioStationGrain> groundStationGrain)
    {
        groundStationGrain = mediumGrain.Get().GroundStation.As<RadioStationGrain>();
        var groundOperator = TestUtility.MockGrain<IAIRadioOperatorGrain>();
        
        var conversationToken1 = groundStationGrain.Get().EnqueueAIOperatorForTransmission(
            groundOperator.Grain,
            priority: AirGroundPriority.FlightSafetyNormal);

        var transmission1 = TestUtility.NewTransmission();
        groundStationGrain.Get().BeginTransmission(transmission1, conversationToken1);

        return new(transmission1, conversationToken1);
    }

    private void ConfigureSiloForTest(SiloConfigurationBuilder config)
    {
        RadioStationGrain.RegisterGrainType(config);
        GroundStationRadioMediumGrain.RegisterGrainType(config);
    }
}
