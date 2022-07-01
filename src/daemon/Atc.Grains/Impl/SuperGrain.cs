using System.Collections.Immutable;

namespace Atc.Grains.Impl;

public class SuperGrain : AbstractGrain<SuperGrain.GrainState>, ISiloGrains, ISiloTimeTravel
{
    public static readonly string TypeString = "$$SUPER";

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
        string GrainId,
        string GrainType,
        IGrainActivationEvent ActivationEvent,
        ulong ActivationEventSequenceNo
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
            grainId: $"{TypeString}/#1", 
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

    public GrainRef<T> CreateGrain<T>(ActivationEventFactory<IGrainActivationEvent<T>> activationEventFactory) where T : class, IGrainId
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

    public void DeleteGrain<T>(GrainRef<T> grainRef) where T : class, IGrainId
    {
        if (!_grainInstanceById.TryGetValue(grainRef.GrainId, out var grainInstance))
        {
            throw new GrainNotFoundException($"Grain '{grainRef.GrainId}' not found");
        }

        if (!(grainInstance is T))
        {
            throw new GrainTypeMismatchException(
                $"Expected grain '{grainRef.GrainId}' to be '{typeof(T).Name}', but found '{grainInstance.GetType().Name}'");
        }
            
        Dispatch(new DeactivateGrainEvent(grainRef.GrainId));    
    }

    public bool TryGetRefById<T>(string grainId, out GrainRef<T>? grainRef) where T : class, IGrainId
    {
        if (TryGetInstanceById<T>(grainId, out var grainInstance))
        {
            grainRef = new GrainRef<T>(this, grainInstance!.GrainId);
            return true;
        }

        grainRef = null;
        return false;
    }

    public bool TryGetInstanceById<T>(string grainId, out T? grainInstance) where T : class, IGrainId
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

    public IEnumerable<GrainRef<T>> GetAllGrainsOfType<T>() where T : class, IGrainId
    {
        return _grainInstanceById
            .Values
            .Where(grain => grain is T)
            .Select(grain => new GrainRef<T>(this, grain.GrainId));    
    }

    public GrainRef<T> GetRefToGrainInstance<T>(T grainInstance) where T : class, IGrainId
    {
        return new GrainRef<T>(this, grainInstance.GrainId);
    }

    public SiloSnapshot TakeSnapshot()
    {
        var grainSnapshots = _grainInstanceById.Values
            .Select(TakeGrainSnapshot)
            .ToImmutableList();

        var opaqueData = new SiloSnapshotOpaqueData(
            NextDispatchSequenceNo: _dispatch.NextSequenceNo,
            Grains: grainSnapshots,
            LastInstanceIdPerTypeString: State.LastInstanceIdPerTypeString);

        return new SiloSnapshot(
            NextDispatchSequenceNo: opaqueData.NextDispatchSequenceNo,
            OpaqueData: opaqueData);
        
        GrainSnapshotOpaqueData TakeGrainSnapshot(IGrain grain)
        {
            var entry = State.GrainEntryById[grain.GrainId];
            var snapshot = new GrainSnapshotOpaqueData(
                GrainType: grain.GrainType,
                GrainId: grain.GrainId,
                ActivationEvent: entry.ActivationEvent,
                ActivationEventSequenceNo: entry.ActivationEventSequenceNo,
                State: grain.GetState());
            return snapshot;
        }
    }

    public void RestoreSnapshot(SiloSnapshot snapshot)
    {
        var siloOpaqueData = (SiloSnapshotOpaqueData) snapshot.OpaqueData;
        
        RemoveExtraGrains();
        CreateOrUpdateGrains();

        //TODO: reset dispatch.NextSequenceNo?
        
        var rebuiltState = RebuildState();
        ((IGrain)this).SetState(rebuiltState);
        
        void CreateOrUpdateGrains()
        {
            foreach (var grainSnapshot in siloOpaqueData.Grains)
            {
                var instance = GetOrCreateGrainInstance(grainSnapshot, out var createdNew);
                if (createdNew)
                {
                    _grainInstanceById.Add(instance.GrainId, instance);
                }

                instance.SetState(grainSnapshot.State);
            }
        }

        IGrain GetOrCreateGrainInstance(GrainSnapshotOpaqueData grainSnapshot, out bool createdNew)
        {
            if (_grainInstanceById.TryGetValue(grainSnapshot.GrainId, out var existingGrain))
            {
                createdNew = false;
                return existingGrain;
            }

            var newInstance = InstantiateGrain(grainSnapshot.ActivationEvent, out var typeString);
            if (typeString != grainSnapshot.GrainType)
            {
                throw new GrainTypeMismatchException(
                    $"Expected grain type '{grainSnapshot.GrainType}', but found '{typeString}'.");
            }

            createdNew = true;
            return newInstance;
        }
        
        GrainState RebuildState()
        {
            var rebuiltEntryById = siloOpaqueData.Grains
                .Select(g => new GrainEntry(
                    GrainId: g.GrainId,
                    GrainType: g.GrainType,
                    ActivationEvent: g.ActivationEvent,
                    ActivationEventSequenceNo: g.ActivationEventSequenceNo))
                .ToImmutableDictionary(g => g.GrainId);
            
            return new GrainState(
                GrainEntryById: rebuiltEntryById,
                LastInstanceIdPerTypeString: siloOpaqueData.LastInstanceIdPerTypeString
            );
        }
        
        void RemoveExtraGrains()
        {
            var extraGrainIds = new HashSet<string>(_grainInstanceById.Keys);
            extraGrainIds.ExceptWith(siloOpaqueData.Grains.Select(g => g.GrainId));

            foreach (var id in extraGrainIds)
            {
                var grain = _grainInstanceById[id];
                DisposeGrain(grain);
                _grainInstanceById.Remove(id);
            }
        }
    }

    public void ReplayEvents(IEnumerable<GrainEventEnvelope> envelopes)
    {
        foreach (var envelope in envelopes)
        {
            if (envelope.SequenceNo != _dispatch.NextSequenceNo)
            {
                throw new EventOutOfSequenceException(
                    $"Expected event sequence no. to be {_dispatch.NextSequenceNo}, but got #{envelope.SequenceNo}");
            }
                
            if (!_grainInstanceById.TryGetValue(envelope.TargetGrainId, out var targetGrain))
            {
                throw new GrainNotFoundException(
                    $"Cannot replay event #{envelope.SequenceNo} because target grain '{envelope.TargetGrainId}' not found");
            }
                
            _dispatch.Dispatch(targetGrain, envelope.Event);
        }
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
                        new GrainEntry(
                            GrainId: newGrain.GrainId,
                            GrainType: newGrain.GrainType,
                            ActivationEvent: activation, 
                            ActivationEventSequenceNo: _dispatch.NextSequenceNo)),
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
            catch //(Exception e)
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

    private T TakeLastCreatedGrainOrThrow<T>() where T : class, IGrainId
    {
        var grain = 
            _lastCreatedGrain as T 
            ?? throw new Exception("Internal error: grain was not created or type mismatch.");
        
        _lastCreatedGrain = null;
        return grain;
    }
}
