using System.Collections.Immutable;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications.Poc;

public interface IPocAIControllerGrain : IAIRadioOperatorGrain, IGrainId
{
    //TODO
}

public class PocAIControllerGrain : 
    AIRadioOperatorGrain<PocBrainState>,
    IPocAIControllerGrain
{
    public static readonly string TypeString = nameof(PocAIControllerGrain);

    [NotEventSourced]
    private readonly IMyTelemetry _telemetry;

    public PocAIControllerGrain(
        ISilo silo,
        //ISpeechService speechService,
        IMyTelemetry telemetry,
        PocBrain.IMyTelemetry brainTelemetry,
        AIControllerGrainActivationEvent activation) :
        base(
            silo: silo,
            grainType: TypeString,
            brain: CreateBrain(activation.Callsign.Full, brainTelemetry),
            telemetry: telemetry,
            party: PocPartyDescriptionFactory.CreateParty(activation),
            activation: activation)
    {
        _telemetry = telemetry;
    }

    public new PocBrain Brain => (PocBrain) base.Brain;

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<PocAIControllerGrain, AIControllerGrainActivationEvent>(
            TypeString,
            (activation, context) => new PocAIControllerGrain(
                silo: context.Resolve<ISilo>(),
                telemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<IMyTelemetry>(),
                brainTelemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<PocBrain.IMyTelemetry>(),
                activation: activation
            ));
    }


    private static PocBrain CreateBrain(string callsign, PocBrain.IMyTelemetry brainTelemetry)
    {
        switch (callsign)
        {
            case "Q": return new PocBrainQ(brainTelemetry);
            default: throw new ArgumentException($"Unexpected callsign: '{callsign}'");
        }
    }

    public record AIControllerGrainActivationEvent(
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
    ), IGrainActivationEvent<PocAIControllerGrain>;

    [TelemetryName("PocAIController")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
