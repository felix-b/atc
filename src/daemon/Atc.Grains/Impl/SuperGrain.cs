using System.Collections.Immutable;

namespace Atc.Grains.Impl;

public class SuperGrain : AbstractGrain<SuperGrain.GrainState>, ISiloGrains, ISiloTimeTravel
{
    public static readonly string TypeString = "SUPERGRAIN";

    [NotEventSourced]
    private readonly Dictionary<string, IGrain> _grainInstanceById = new();
    [NotEventSourced]
    private IGrain? _lastCreatedGrain = null;
    
    public record GrainState(
        ImmutableDictionary<string, GrainEntry> GrainEntryById,
        ImmutableDictionary<string, ulong> LastInstanceIdPerTypeString)
    {
        public static GrainState CreateInitial() => new GrainState(
            GrainEntryById: ImmutableDictionary<string, GrainEntry>.Empty,
            LastInstanceIdPerTypeString: ImmutableDictionary<string, ulong>.Empty);
    }

    public record GrainEntry(
        IGrainActivationEvent ActivationEvent,
        ulong SequenceNo
    );
    
    public record DeactivateGrainEvent(
        string GrainId
    ) : IGrainEvent;

    private readonly Dictionary<Type, GrainTypeRegistration> _registrationByGrainType;
    private readonly Dictionary<Type, GrainTypeRegistration> _registrationByActivationEventType;
    private readonly ISiloEventDispatch _dispatch;
    private readonly ISiloTelemetry _telemetry;
    private readonly Func<ISiloDependencyContext> _getDependencyContext;
    
    public SuperGrain(
        ISiloEventDispatch dispatch, 
        ISiloTelemetry telemetry, 
        Func<ISiloDependencyContext> getDependencyContext,
        IReadOnlyCollection<GrainTypeRegistration> grainTypeRegistrations) 
        : base(
            grainId: "#SUPER", 
            grainType: TypeString, 
            dispatch, 
            initialState: GrainState.CreateInitial())
    {
        _registrationByGrainType = grainTypeRegistrations.ToDictionary(r => r.GrainClrType);
        _registrationByActivationEventType = grainTypeRegistrations.ToDictionary(r => r.ActivationEventClrType);
        _dispatch = dispatch;
        _telemetry = telemetry;
        _getDependencyContext = getDependencyContext;
    }

    public GrainRef<T> CreateGrain<T>(ActivationEventFactory<IGrainActivationEvent<T>> activationEventFactory) where T : class, IGrain
    {
        if (!_registrationByGrainType.TryGetValue(typeof(T), out var registration))
        {
            throw new GrainTypeNotFoundException($"Grain with CLR type '{typeof(T).Name}' was not registered");
        }

        var instanceId = GetNextInstanceId(registration.GrainTypeString);
        var uniqueId = $"{registration.GrainTypeString}/#{instanceId}";
        var activationEvent = activationEventFactory(uniqueId);
            
        _dispatch.Dispatch(this, activationEvent);

        var grain = TakeLastCreatedGrainOrThrow<T>(); 
        (grain as IStartableGrain)?.Start();
            
        return new GrainRef<T>(this, grain.GrainId);
    }

    public void DeleteGrain<T>(GrainRef<T> grain) where T : class, IGrain
    {
        throw new NotImplementedException();
    }

    public bool TryGetGrainById<T>(string grainId, out GrainRef<T>? grainRef) where T : class, IGrain
    {
        if (TryGetGrainObjectById<T>(grainId, out var grainInstance))
        {
            grainRef = new GrainRef<T>(this, grainInstance!.GrainId);
            return true;
        }

        grainRef = null;
        return false;
    }

    public bool TryGetGrainObjectById<T>(string grainId, out T? grainInstance) where T : class, IGrain
    {
        var result = _grainInstanceById.TryGetValue(grainId, out var nonTypedInstance);
        if (result && nonTypedInstance is T typedInstance)
        {
            grainInstance = typedInstance;
            return true;
        }

        grainInstance = null;
        return false;
    }

    public IEnumerable<GrainRef<T>> GetAllGrainsOfType<T>() where T : class, IGrain
    {
        return _grainInstanceById
            .Values
            .OfType<T>()
            .Select(grain => new GrainRef<T>(this, grain.GrainId));    
    }

    public GrainRef<T> GetRefToGrainInstance<T>(T grainInstance) where T : class, IGrain
    {
        return new GrainRef<T>(this, grainInstance.GrainId);
    }

    public ISiloSnapshot TakeSnapshot()
    {
        throw new NotImplementedException();
    }

    public void RestoreSnapshot(ISiloSnapshot snapshot)
    {
        throw new NotImplementedException();
    }

    public void ReplayEvents(IEnumerable<IGrainEvent> events)
    {
        throw new NotImplementedException();
    }

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case IGrainActivationEvent activation:
                var newGrain = InstantiateGrain(activation, out var typeString);
                if (!stateBefore.LastInstanceIdPerTypeString.TryGetValue(typeString, out var lastInstanceId))
                {
                    lastInstanceId = 0;
                }
                _lastCreatedGrain = newGrain;
                _grainInstanceById[newGrain.GrainId] = newGrain;
                return stateBefore with {
                    GrainEntryById = stateBefore.GrainEntryById.Add(
                        activation.GrainId, 
                        new GrainEntry(activation, _dispatch.NextSequenceNo)),
                    LastInstanceIdPerTypeString = stateBefore.LastInstanceIdPerTypeString.SetItem(typeString, lastInstanceId + 1),
                };
            case DeactivateGrainEvent deactivation:
                if (!stateBefore.GrainEntryById.ContainsKey(deactivation.GrainId) || 
                    !_grainInstanceById.TryGetValue(deactivation.GrainId, out var grainToDelete))
                {
                    throw new GrainNotFoundException($"Failed to deactivate grain '{deactivation.GrainId}': no such grain");
                }
                DisposeGrain(grainToDelete);
                _grainInstanceById.Remove(deactivation.GrainId);
                return stateBefore with {
                    GrainEntryById = stateBefore.GrainEntryById.Remove(deactivation.GrainId)
                };
            default:
                return stateBefore;
        }
    }

    private IGrain InstantiateGrain(IGrainActivationEvent @event, out string typeString)
    {
        if (!_registrationByActivationEventType.TryGetValue(@event.GetType(), out var registration))
        {
            throw new GrainTypeNotFoundException($"Activation event of type '{@event.GetType().Name}' was not registered");
        }

        typeString = registration.GrainTypeString;
        return registration.NonTypedFactory(@event, _getDependencyContext());
    }

    private void DisposeGrain(IGrain grain)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (grain is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                //TODO: log error
            }
        }
    }
    
    private ulong GetNextInstanceId(string typeString)
    {
        return State.LastInstanceIdPerTypeString.TryGetValue(typeString, out var id)
            ? id + 1
            : 1;
    }

    private T TakeLastCreatedGrainOrThrow<T>() where T : class, IGrain
    {
        var grain = 
            _lastCreatedGrain as T 
            ?? throw new Exception("Internal error: grain was not created or type mismatch.");
        
        _lastCreatedGrain = null;
        return grain;
    }
}
