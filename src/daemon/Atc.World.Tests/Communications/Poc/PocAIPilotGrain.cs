using System.Collections.Immutable;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications.Poc;

public interface IPocAIPilotGrain : IAIRadioOperatorGrain, IGrainId
{
    //TODO
}

public class PocAIPilotGrain : 
    AIRadioOperatorGrain<PocBrainState>,
    IPocAIPilotGrain
{
    public static readonly string TypeString = nameof(PocAIPilotGrain);

    [NotEventSourced]
    private readonly IMyTelemetry _telemetry;

    public PocAIPilotGrain(
        ISilo silo,
        //ISpeechService speechService,
        IMyTelemetry telemetry,
        PocBrain.IMyTelemetry brainTelemetry,
        AIPilotGrainActivationEvent activation) :
        base(
            silo: silo,
            grainType: TypeString,
            brain: CreateBrain(activation.Callsign, brainTelemetry),
            telemetry: telemetry,
            party: PocPartyDescriptionFactory.CreateParty(activation),
            activation: activation)
    {
        _telemetry = telemetry;
    }

    public new PocBrain Brain => (PocBrain) base.Brain;

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<PocAIPilotGrain, AIPilotGrainActivationEvent>(
            TypeString,
            (activation, context) => new PocAIPilotGrain(
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
            case "A": return new PocBrainA(brainTelemetry);
            case "B": return new PocBrainB(brainTelemetry);
            case "C": return new PocBrainC(brainTelemetry);
            case "D": return new PocBrainD(brainTelemetry);
            default: throw new ArgumentException($"Unexpected callsign: '{callsign}'");
        }
    }

    public record AIPilotGrainActivationEvent(
        string GrainId,
        string Callsign,
        GrainRef<IWorldGrain> World,
        GrainRef<IRadioStationGrain> Radio
    ) : AIRadioOperatorGrain<PocBrainState>.GrainActivationEvent(
        GrainId: GrainId,
        Callsign: Callsign,
        World: World,
        Radio: Radio,
        Language: LanguageCode.English
    ), IGrainActivationEvent<PocAIPilotGrain>;

    [TelemetryName("PocAIPilot")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
