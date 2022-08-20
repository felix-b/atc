using Atc.Grains;
using Atc.Maths;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;

namespace Atc.World.LLLL;

public class LlhzAITowerControllerGrain : 
    AIRadioOperatorGrain<LlhzTowerControllerBrain.BrainState>, 
    ILlhzControllerGrain
{
    public static readonly string TypeString = nameof(LlhzAITowerControllerGrain);

    [NotEventSourced]
    private readonly IMyTelemetry _telemetry;

    public LlhzAITowerControllerGrain(
        ISilo silo,
        IMyTelemetry telemetry,
        LlhzTowerControllerBrain.IMyTelemetry brainTelemetry,
        GrainActivationEvent activation) :
        base(
            silo,
            telemetry,
            grainType: TypeString,
            brain: new LlhzTowerControllerBrain(activation.Callsign, brainTelemetry),
            party: LlhzPartyDescriptionFactory.CreateParty(activation),
            activation)
    {
        _telemetry = telemetry;
    }

    public void AcceptHandoff(LlhzHandoffIntent handoff)
    {
        InvokeBrain(new IntentTuple(handoff));
    }

    public Callsign Callsign => State.Callsign;
    public ControllerPositionType PositionType => ControllerPositionType.Tower;
    public Frequency Frequency => State.Radio.Get().Frequency;

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<LlhzAITowerControllerGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new LlhzAITowerControllerGrain(
                silo: context.Resolve<ISilo>(),
                telemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<IMyTelemetry>(),
                brainTelemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<LlhzTowerControllerBrain.IMyTelemetry>(),
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
    ), IGrainActivationEvent<LlhzAITowerControllerGrain>;

    [TelemetryName("LlhzAITowerController")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
