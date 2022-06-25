using Atc.Grains.Impl;

namespace Atc.Grains;

public class SiloConfigurationBuilder
{
    private readonly Dictionary<string, GrainTypeRegistration> _grainRegistrationByType = new();

    public SiloConfigurationBuilder(
        ISiloTelemetry telemetry, 
        ISiloEventStreamWriter eventWriter, 
        ISiloDependencyBuilder dependencyBuilder,
        ISiloEnvironment environment)
    {
        Telemetry = telemetry;
        EventWriter = eventWriter;
        DependencyBuilder = dependencyBuilder;
        Environment = environment;
    }

    public void RegisterGrainType<TGrain, TActivationEvent>(
        string grainTypeString, 
        GrainTypedFactory<TGrain, TActivationEvent> factory)
        where TGrain : class, IGrain
        where TActivationEvent : class, IGrainActivationEvent
    {
        var registration = new GrainTypeRegistration<TGrain, TActivationEvent>(grainTypeString, factory);
        _grainRegistrationByType.Add(grainTypeString, registration);
    }

    public IReadOnlyCollection<GrainTypeRegistration> GrainTypeRegistrations => _grainRegistrationByType.Values;

    public ISiloTelemetry Telemetry { get; }
    public ISiloEventStreamWriter EventWriter { get; }
    public ISiloDependencyBuilder DependencyBuilder { get; }
    public ISiloEnvironment Environment { get; }
}

public abstract record GrainTypeRegistration(
    string GrainTypeString,
    Type GrainClrType,
    Type ActivationEventClrType,
    GrainNonTypedFactory NonTypedFactory);

public record GrainTypeRegistration<TGrain, TActivationEvent>(
    string GrainTypeString,
    Type GrainClrType,
    Type ActivationEventClrType,
    GrainTypedFactory<TGrain, TActivationEvent> TypedFactory,
    GrainNonTypedFactory NonTypedFactory) 
    : GrainTypeRegistration(GrainTypeString, typeof(TGrain), typeof(TActivationEvent), NonTypedFactory)
    where TGrain : class, IGrain
    where TActivationEvent : class, IGrainActivationEvent
{
    public GrainTypeRegistration(string grainTypeString, GrainTypedFactory<TGrain, TActivationEvent> typedFactory)
        : this(
            grainTypeString, 
            GrainClrType: typeof(TGrain), 
            ActivationEventClrType: typeof(TActivationEvent), 
            TypedFactory: typedFactory,
            NonTypedFactory: CreateNonTypedFactory(typedFactory))
    {
    }

    public static GrainNonTypedFactory CreateNonTypedFactory(
        GrainTypedFactory<TGrain, TActivationEvent> typedFactory)
    {
        return (activation, context) => typedFactory((TActivationEvent) activation, context);
    }
}
