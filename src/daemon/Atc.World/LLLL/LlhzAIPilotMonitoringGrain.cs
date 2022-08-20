using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.LLLL;

public interface ILlhzAIPilotMonitoringGrain : IAIRadioOperatorGrain
{
    void AcceptPilotFlyingUpdate(string state, string trigger);
}

public class LlhzAIPilotMonitoringGrain : 
    AIRadioOperatorGrain<LlhzPilotBrain.BrainState>,
    ILlhzAIPilotMonitoringGrain
{
    public static readonly string TypeString = nameof(LlhzAIPilotMonitoringGrain);

    [NotEventSourced]
    private readonly IMyTelemetry _telemetry;

    public LlhzAIPilotMonitoringGrain(
        ISilo silo,
        IMyTelemetry telemetry,
        LlhzPilotBrain.IMyTelemetry brainTelemetry,
        GrainActivationEvent activation) :
        base(
            silo,
            telemetry,
            grainType: TypeString,
            brain: new LlhzPilotBrain(activation.Callsign, brainTelemetry),
            party: LlhzPartyDescriptionFactory.CreateParty(activation),
            activation)
    {
        _telemetry = telemetry;
    }

    public void AcceptPilotFlyingUpdate(string state, string trigger)
    {
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<LlhzAIPilotMonitoringGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new LlhzAIPilotMonitoringGrain(
                silo: context.Resolve<ISilo>(),
                telemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<IMyTelemetry>(),
                brainTelemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<LlhzPilotBrain.IMyTelemetry>(),
                activation: activation
            ));
    }

    public record GrainActivationEvent(
        string GrainId,
        Callsign Callsign,
        GrainRef<IWorldGrain> World,
        GrainRef<IRadioStationGrain> Radio
    ) : GrainActivationEventBase(
        GrainId: GrainId,
        Callsign: Callsign,
        World: World,
        Radio: Radio,
        Language: LanguageCode.English
    ), IGrainActivationEvent<LlhzAIPilotMonitoringGrain>;

    [TelemetryName("LlhzAIPilot")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
