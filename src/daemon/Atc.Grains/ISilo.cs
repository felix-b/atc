using Atc.Grains.Impl;

namespace Atc.Grains;

public interface ISilo
{
    string SiloId { get; }

    ISiloGrains Grains { get; }

    ISiloEventDispatch Dispatch { get; }

    ISiloTaskQueue TaskQueue { get; }

    ISiloTimeTravel TimeTravel { get; }
    
    ISiloTelemetry Telemetry { get; }
    
    public static ISilo Create(
        string siloId,
        SiloConfigurationBuilder configuration,
        Action<SiloConfigurationBuilder>? configure = null)
    {
        configure?.Invoke(configuration);
        var silo = new Silo(siloId, configuration);
        return silo;
    }
}

public interface ISiloGrains
{
    GrainRef<T> CreateGrain<T>(ActivationEventFactory<IGrainActivationEvent<T>> activationEventFactory)
        where T : class, IGrain;

    void DeleteGrain<T>(GrainRef<T> grain)
        where T : class, IGrain;

    bool TryGetGrainById<T>(string grainId, out GrainRef<T>? grainRef)
        where T : class, IGrain;

    bool TryGetGrainObjectById<T>(string grainId, out T? grainInstance)
        where T : class, IGrain;

    IEnumerable<GrainRef<T>> GetAllGrainsOfType<T>()
        where T : class, IGrain;
        
    GrainRef<T> GetRefToGrainInstance<T>(T grainInstance)
        where T : class, IGrain;
        
    public GrainRef<T> GetGrainByIdOrThrow<T>(string grainId)
        where T : class, IGrain
    {
        if (TryGetGrainById<T>(grainId, out var actor))
        {
            return actor!.Value;
        }

        throw new GrainNotFoundException($"Grain '{grainId}' could not be found");
    }
    
    public T GetGrainObjectByIdOrThrow<T>(string grainId) where T : class, IGrain
    {
        if (TryGetGrainObjectById<T>(grainId, out var grain))
        {
            return grain!;
        }

        throw new GrainNotFoundException($"Grain '{grainId}' could not be found");
    }    
}

public interface ISiloEventDispatch
{
    Task Dispatch(IGrain target, IGrainEvent @event);
    ulong NextSequenceNo { get; }
}

public interface ISiloTaskQueue
{
    
}

public interface ISiloTimeTravel
{
    ISiloSnapshot TakeSnapshot();
    void RestoreSnapshot(ISiloSnapshot snapshot);
    void ReplayEvents(IEnumerable<IGrainEvent> events);
}

public interface ISiloSnapshot
{
    ulong NextEventSequenceNo { get; }
}

public interface ISiloDependencyBuilder
{
    void AddSingleton<T>(T singletonInstance);
    void AddTransient<T>(Func<T> transientFactory);
    ISiloDependencyContext GetContext();
}

public interface ISiloDependencyContext
{
    T Resolve<T>() where T : class;
}

public delegate TActivationEvent ActivationEventFactory<TActivationEvent>(string grainId)
    where TActivationEvent : class, IGrainActivationEvent;

public delegate IGrain GrainNonTypedFactory(
    IGrainActivationEvent constructorEvent, 
    ISiloDependencyContext dependencies);

public delegate TGrain GrainTypedFactory<TGrain, TActivationEvent>(
    TActivationEvent constructorEvent, 
    ISiloDependencyContext dependencies)
    where TGrain : class, IGrain
    where TActivationEvent : class, IGrainActivationEvent;

