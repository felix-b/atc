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
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
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
        state.LastKnownLocation.Should().Be(Location.Create(10f, 20f, 100f));
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
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.Create(10f, 20f, 100f));
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
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.Create(10f, 20f, 100f));
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
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.Create(10f, 20f, 100f));
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
    public void CanTuneMobileStationWithMediumToNewFrequencyWithoutMedium()
    {
        //-- given 
        
        var silo = SiloTestDoubles.CreateSilo("test", ConfigureSiloForTest);
        var world = TestUtility.MockWorldGrain(silo);
        var stationGrain = CreateMobileStationWithMediumSetup(silo, world, out var mediumGrain);

        stationGrain.Get().TurnOnMobileStation(
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
            Frequency.FromKhz(118000));

        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(11f, 21f),
            Altitude.FromFeetMsl(200f),
            Frequency.FromKhz(119000)
        )).Returns((GrainRef<IGroundStationRadioMediumGrain>?)null);
        
        //-- when 
        
        stationGrain.Get().TuneMobileStation(        
            Location.Create(lat: 11f, lon: 21f, elevationFeetMsl: 200f), 
            Frequency.FromKhz(119000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(119000));
        state.LastKnownLocation.Should().Be(Location.Create(11f, 21f, 200f));
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
            Location.Create(lat: 11f, lon: 21f, elevationFeetMsl: 200f), 
            Frequency.FromKhz(119000));

        //-- when 
        
        stationGrain.Get().TuneMobileStation(        
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
            Frequency.FromKhz(118000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(stationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(118000));
        state.LastKnownLocation.Should().Be(Location.Create(10f, 20f, 100f));
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
            Location.Create(lat: 10f, lon: 20f, elevationFeetMsl: 100f), 
            Frequency.FromKhz(118000));

        //-- when 
        
        mobileStationGrain.Get().TuneMobileStation(        
            Location.Create(lat: 11f, lon: 21f, elevationFeetMsl: 200f), 
            Frequency.FromKhz(119000));

        //-- then
        
        var state = SiloTestDoubles.GetGrainState(mobileStationGrain.Get());
        state.StationType.Should().Be(RadioStationType.Mobile);
        state.SelectedFrequency.Should().Be(Frequency.FromKhz(119000));
        state.LastKnownLocation.Should().Be(Location.Create(11f, 21f, 200f));
        state.PttPressed.Should().BeFalse();
        state.Status.Should().Be(TransceiverStatus.ReceivingSingleTransmission);
        state.CurrentTransmission.Should().BeSameAs(transmission1);
        state.ConversationToken.Should().BeSameAs(conversationToken1);
        state.GroundStationMedium!.Value.Should().Be(mediumGrain2);
        state.Listeners.Should().BeEquivalentTo(new[] {
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

    private GrainRef<RadioStationGrain> CreateMobileStationWithMediumSetup(
        ISilo silo, 
        MockedGrain<IWorldGrain> world, 
        out GrainRef<GroundStationRadioMediumGrain> mediumGrain)
    {
        var stationGrain = silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Mobile));

        mediumGrain = silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(
            grainId => new GroundStationRadioMediumGrain.GrainActivationEvent(grainId));

        world.Mock.Setup(x => x.TryFindRadioMedium(
            GeoPoint.LatLon(10f, 20f),
            Altitude.FromFeetMsl(100f),
            Frequency.FromKhz(118000)
        )).Returns(mediumGrain.As<IGroundStationRadioMediumGrain>());

        return stationGrain;
    }

    private void SetupInProgressTransmission(
        GrainRef<GroundStationRadioMediumGrain> mediumGrain, 
        out ConversationToken conversationToken,
        out TransmissionDescription transmission)
    {
        conversationToken = new ConversationToken(1, AirGroundPriority.FlightSafetyNormal);
        transmission = new TransmissionDescription();

        var otherMobileStation = TestUtility.MockGrain<IRadioStationGrain>();
        var mediumState = SiloTestDoubles.GetGrainState(mediumGrain.Get());

        SiloTestDoubles.SetGrainState(mediumGrain.Get(), mediumState with {
            InProgressTransmissionByStationId =
            ImmutableDictionary<string, GroundStationRadioMediumGrain.InProgressTransmissionEntry>
                .Empty
                .Add(otherMobileStation.Grain.GrainId, new GroundStationRadioMediumGrain.InProgressTransmissionEntry(
                    otherMobileStation.Grain,
                    conversationToken,
                    transmission
                ))
        });
    }

    private void ConfigureSiloForTest(SiloConfigurationBuilder config)
    {
        RadioStationGrain.RegisterGrainType(config);
        GroundStationRadioMediumGrain.RegisterGrainType(config);
    }
}
