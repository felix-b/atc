using Atc.Grains;
using Atc.Maths;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;

namespace Atc.World.LLLL;

public class LlhzAIClearanceControllerGrain : 
    AIRadioOperatorGrain<LlhzClearanceControllerBrain.BrainState>, 
    ILlhzControllerGrain
{
    public static readonly string TypeString = nameof(LlhzAIClearanceControllerGrain);

    [NotEventSourced]
    private readonly IMyTelemetry _telemetry;

    public LlhzAIClearanceControllerGrain(
        ISilo silo,
        IMyTelemetry telemetry,
        LlhzClearanceControllerBrain.IMyTelemetry brainTelemetry,
        GrainActivationEvent activation) :
        base(
            silo,
            telemetry,
            grainType: TypeString,
            brain: new LlhzClearanceControllerBrain(activation.Callsign, brainTelemetry),
            party: LlhzPartyDescriptionFactory.CreateParty(activation),
            activation)
    {
        _telemetry = telemetry;
    }

    public void AcceptHandoff(LlhzHandoffIntent handoff)
    {
        InvokeBrain(new IntentTuple(handoff));
    }

    public ControllerPositionType PositionType => ControllerPositionType.ClearanceDelivery;
    public Callsign Callsign => State.Callsign;
    public Frequency Frequency => State.Radio.Get().Frequency;

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<LlhzAIClearanceControllerGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new LlhzAIClearanceControllerGrain(
                silo: context.Resolve<ISilo>(),
                telemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<IMyTelemetry>(),
                brainTelemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<LlhzClearanceControllerBrain.IMyTelemetry>(),
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
    ), IGrainActivationEvent<LlhzAIClearanceControllerGrain>;

    [TelemetryName("LlhzAIClearanceController")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
