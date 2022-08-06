using System.Collections.Immutable;
using System.Reflection;
using Atc.Grains.Impl;
using Atc.Telemetry;

namespace Atc.Grains;

public static class SiloTestDoubles
{
    public static ISilo CreateSilo(
        string siloId,
        Action<SiloConfigurationBuilder> configure,
        ISiloTelemetry? telemetry = null,
        ISiloEventStreamWriter? eventWriter = null,
        ISiloDependencyBuilder? dependencies = null,
        ISiloEnvironment? environment = null)
    {
        var silo = ISilo.Create(
            siloId,
            configuration: new SiloConfigurationBuilder(
                telemetry ?? new TestTelemetry(), 
                eventWriter ?? new TestEventStreamWriter(), 
                dependencies ?? new TestDependencyContext(),
                environment ?? new TestEnvironment()),
            configure);
        
        return silo;
    }

    public static TState GetGrainState<TState>(AbstractGrain<TState> grain) 
        where TState : class
    {
        return (TState)((IGrain)grain).GetState();
    }

    public static void SetGrainState<TState>(AbstractGrain<TState> grain, TState state) 
        where TState : class
    {
        ((IGrain)grain).SetState(state);
    }

    public static TState DispatchGrainEvent<TState>(AbstractGrain<TState> grain, IGrainEvent @event) 
        where TState : class
    {
        grain.GetDispatch().Dispatch(grain, @event);
        return (TState)((IGrain)grain).GetState();
    }

    public static TState InvokeGrainReduce<TState>(AbstractGrain<TState> grain, TState stateBefore, IGrainEvent @event) 
        where TState : class
    {
        var infraGrain = (IGrain) grain;
        var stateAfter = infraGrain.Reduce(stateBefore, @event);
        return (TState)stateAfter;
    }

    public static bool InvokeGrainWorkItem<TState>(
        AbstractGrain<TState> grain,
        TState stateBefore,
        IGrainWorkItem workItem, 
        bool timedOut,
        out TState stateAfter)
        where TState : class
    {
        var infraGrain = (IGrain) grain;
        infraGrain.SetState(stateBefore);
        
        var handled = infraGrain.ExecuteWorkItem(workItem, timedOut);
        stateAfter = (TState)infraGrain.GetState();

        return handled;
    }

    public static IEnumerable<IGrainWorkItem> GetWorkItemsInTaskQueue(ISilo silo)
    {
        var taskQueueGrain = silo.Grains.GetAllGrainsOfType<TaskQueueGrain>().First();
        return taskQueueGrain.Get().WorkItems;
    }

    public static GrainRef<T> MockGrainRef<T>(T instance) where T : class, IGrainId
    {
        var superGrainMock = new MockSuperGrainForGrainRef(instance);
        return new GrainRef<T>(superGrainMock, instance.GrainId);
    }

    public static void InjectGrainMockToSilo(IGrain grainMock, ISilo injectTo)
    {
        ((SuperGrain)injectTo.Grains).InjectGrainInstance(grainMock);
    }

    public class TestEventStreamWriter : ISiloEventStreamWriter
    {
        public ImmutableList<GrainEventEnvelope> Events { get; private set; } = ImmutableList<GrainEventEnvelope>.Empty;

        public void FireGrainEvent(GrainEventEnvelope envelope)
        {
            Events = Events.Add(envelope);
        }
    }
    
    public class TestDependencyContext : ISiloDependencyBuilder, ISiloDependencyContext
    {
        private readonly Dictionary<Type, object> _singletonInstanceByType = new();
        private readonly Dictionary<Type, Delegate> _transientFactoryByType = new();

        public void AddSingleton<T>(T singletonInstance)
        {
            _singletonInstanceByType[typeof(T)] = singletonInstance!;
        }

        public void AddTransient<T>(Func<T> factory)
        {
            _transientFactoryByType[typeof(T)] = factory;
        }

        public ISiloDependencyContext GetContext()
        {
            return this;
        }

        T ISiloDependencyContext.Resolve<T>() where T : class
        {
            if (_singletonInstanceByType.TryGetValue(typeof(T), out var untypedInstance))
            {
                return (T) untypedInstance;
            }

            var factory = (Func<T>) _transientFactoryByType[typeof(T)];
            return factory();
        }
    }
    
    public class TestEnvironment : ISiloEnvironment
    {
        private DateTime? _presetUtcNow = null;
        private string _assetRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        public string GetAssetFilePath(string relativePath)
        {
            return Path.Combine(_assetRootPath, relativePath);
        }

        public void SetAssetRootPath(string path)
        {
            _assetRootPath = path;
        }

        public DateTime UtcNow
        {
            get => _presetUtcNow ?? DateTime.UtcNow;
            set => _presetUtcNow = value;
        }
    }
    
    public class TestTelemetry : TelemetryTestDoubleBase, ISiloTelemetry
    {
        public void DebugDispatchEvent(string grainId, IGrainEvent @event)
        {
            ReportDebug($"DispatchEvent [{grainId}] [{@event.GetType().Name}]");
        }

        public ITraceSpan SpanExecuteReadyWorkItems()
        {
            return new TestSpan(this, $"ExecuteReadyWorkItems");
        }

        public ITraceSpan SpanExecuteWorkItem(string grainId, string workItemType, ulong workItemId, bool timedOut)
        {
            return new TestSpan(this, $"ExecuteWorkItem [{grainId}] type[{workItemType}] id[{workItemId}] timedOut[{timedOut}]");
        }

        public void DebugInsertWorkItem(
            ulong id, string type, DateTime notEarlierThanUtc, DateTime notLaterThanUtc, DateTime firstWorkItemUtc)
        {
            ReportDebug("InsertWorkItem");
        }

        public void DebugRemoveWorkItem(ulong id, DateTime firstWorkItemUtc)
        {
            ReportDebug("RemoveWorkItem");
        }

        public EventFailedException ExceptionDispatchEventFailed(ulong sequenceNo, string targetGrainId, string eventType, Exception exception)
        {
            var message = $"DispatchEventFailed seqNop[{sequenceNo}] targetGrainId[{targetGrainId}] eventType[{eventType}] exception[{exception.GetType().Name}: {exception.Message}]";
            ReportError(message);
            return new EventFailedException(message);
        }

        public InvalidOperationException ExceptionInvalidInteractionThread(string siloId, int ownerThreadId, int currentThreadId)
        {
            var message = $"InvalidInteractionThread siloId[{siloId}] ownerThreadId[{ownerThreadId}] currentThreadId[{currentThreadId}]";
            ReportError(message);
            return new InvalidOperationException(message);
        }

        public void DebugPostedAsyncAction(ulong key)
        {
            ReportDebug($"PostedAsyncAction key[{key}]");
        }

        public void ErrorFailedToPostAsyncActionQueueFull(ulong key)
        {
            ReportDebug($"FailedToPostAsyncActionQueueFull key[{key}]");
        }

        public ITraceSpan SpanRunAsyncAction(ulong key)
        {
            return new TestSpan(this, $"SpanRunAsyncAction key[{key}]");
        }

        public void ErrorFailedToExecuteAsyncAction(ulong key, Exception exception)
        {
            ReportError($"FailedToExecuteAsyncAction key[{key}], exception[${exception.GetType().Name}: {exception.Message}]");
        }

        public void ErrorExecuteReadyWorkItemsFailed(Exception exception)
        {
            ReportError($"ExecuteReadyWorkItemsFailed exception[${exception.GetType().Name}: {exception.Message}]");
        }
    }

    private class MockSuperGrainForGrainRef : ISiloGrains
    {
        private readonly IGrainId _instance;

        public MockSuperGrainForGrainRef(IGrainId instance)
        {
            _instance = instance;
        }

        public GrainRef<T> CreateGrain<T>(ActivationEventFactory<IGrainActivationEvent<T>> activationEventFactory) where T : class, IGrainId
        {
            throw new NotSupportedException(nameof(MockSuperGrainForGrainRef));
        }

        public void DeleteGrain<T>(GrainRef<T> grainRef) where T : class, IGrainId
        {
            throw new NotSupportedException(nameof(MockSuperGrainForGrainRef));
        }

        public bool TryGetRefById<T>(string grainId, out GrainRef<T>? grainRef) where T : class, IGrainId
        {
            throw new NotSupportedException(nameof(MockSuperGrainForGrainRef));
        }

        public bool TryGetInstanceById<T>(string grainId, out T? grainInstance) where T : class, IGrainId
        {
            if (_instance.GrainId == grainId)
            {
                grainInstance = (T)_instance;
                return true;
            }
            
            grainInstance = default;
            return false;
        }

        public IEnumerable<GrainRef<T>> GetAllGrainsOfType<T>() where T : class, IGrainId
        {
            throw new NotSupportedException(nameof(MockSuperGrainForGrainRef));
        }

        public GrainRef<T> GetRefToGrainInstance<T>(T grainInstance) where T : class, IGrainId
        {
            throw new NotSupportedException(nameof(MockSuperGrainForGrainRef));
        }
    }
}
