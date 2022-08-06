using System.Collections.Immutable;
using System.Threading.Channels;

namespace Atc.Grains.Impl;

public class TaskQueueGrain : AbstractGrain<TaskQueueGrain.GrainState>, ISiloTaskQueue
{
    public static readonly string TypeString = "$$TASKQ";
    public static readonly DateTime EmptyQueueFirstWorkItemUtc = DateTime.MaxValue; 

    public record GrainState(
        ImmutableSortedSet<WorkItemEntry> Entries,
        DateTime FirstWorkItemUtc,
        ulong NextWorkItemId)
    {
        public static GrainState CreateInitial()
        {
            var emptySet = ImmutableSortedSet.Create<WorkItemEntry>(new WorkItemEntryComparer()); 
            return new GrainState(
                Entries: emptySet,
                FirstWorkItemUtc: EmptyQueueFirstWorkItemUtc,
                NextWorkItemId: 1
            );
        }
    }

    public record GrainActivationEvent(
        string GrainId
    ) : IGrainActivationEvent<TaskQueueGrain>;

    public record InsertWorkItemEntryEvent(
        DateTime UtcNow,
        WorkItemEntry Entry
    ) : IGrainEvent;

    public record RemoveWorkItemEntryEvent(
        DateTime UtcNow,
        ulong EntryId
    ) : IGrainEvent;

    public record WorkItemEntry(
        ulong Id,
        GrainRef<IGrain> TargetRef,
        IGrainWorkItem WorkItem,
        DateTime? NotEarlierThanUtc,
        DateTime? NotLaterThanUtc,
        bool HasPredicate
    );

    private readonly ISiloGrains _grains;
    private readonly ISiloTelemetry _telemetry;
    private readonly ISiloEnvironment _environment;
    
    [NotEventSourced]
    private readonly Channel<AsyncActionEntry> _postedAsyncActions = Channel.CreateBounded<AsyncActionEntry>(1000);

    public TaskQueueGrain(
        string grainId,
        ISiloGrains grains,
        ISiloEventDispatch dispatch,
        ISiloTelemetry telemetry, 
        ISiloEnvironment environment)
        : base(
            grainId: grainId,
            grainType: TypeString, 
            dispatch, 
            initialState: GrainState.CreateInitial())
    {
        _grains = grains;
        _telemetry = telemetry;
        _environment = environment;
    }

    public GrainWorkItemHandle Defer(
        IGrain target, 
        IGrainWorkItem workItem, 
        DateTime? notEarlierThanUtc = null,
        DateTime? notLaterThanUtc = null,
        bool withPredicate = false)
    {
        var itemId = State.NextWorkItemId;
        var targetRef = _grains.GetRefToGrainInstance(target);
        var entry = new WorkItemEntry(
            itemId, 
            targetRef,
            workItem, 
            NotEarlierThanUtc: notEarlierThanUtc, 
            NotLaterThanUtc: notLaterThanUtc,
            HasPredicate: withPredicate);

        Dispatch(new InsertWorkItemEntryEvent(_environment.UtcNow, entry));
        
        _telemetry.DebugInsertWorkItem(
            id: entry.Id, 
            type: workItem.GetType().Name, 
            notEarlierThanUtc ?? _environment.UtcNow, 
            notLaterThanUtc ?? DateTime.MaxValue, 
            State.FirstWorkItemUtc);
    
        return new GrainWorkItemHandle(itemId);
    }

    public void CancelWorkItem(GrainWorkItemHandle handle)
    {
        Dispatch(new RemoveWorkItemEntryEvent(_environment.UtcNow, handle.Id));

        _telemetry.DebugRemoveWorkItem(
            id: handle.Id, 
            State.FirstWorkItemUtc);
    }

    public void PostAsyncAction(ulong key, Action action)
    {
        if (_postedAsyncActions.Writer.TryWrite(new AsyncActionEntry(key, action)))
        {
            _telemetry.DebugPostedAsyncAction(key);
        }
        else
        {
            _telemetry.ErrorFailedToPostAsyncActionQueueFull(key);
        }
    }

    public void BlockWhileIdle(CancellationToken cancellation)
    {
        var millisecondsToNextWorkItem = (int)NextWorkItemAtUtc.Subtract(_environment.UtcNow).TotalMilliseconds;
        if (millisecondsToNextWorkItem <= 25)
        {
            return;
        }
        
        var awaitAsyncAction = _postedAsyncActions.Reader.WaitToReadAsync(cancellation);
        awaitAsyncAction.AsTask().Wait(millisecondsToNextWorkItem, cancellation);
    }

    public void ExecuteReadyWorkItems()
    {
        var utcNow = _environment.UtcNow;
        using var traceSpan = _telemetry.SpanExecuteReadyWorkItems();

        try
        {
            var readyWorkItemQuery = GetReadyWorkItemQuery();

            foreach (var entry in readyWorkItemQuery)
            {
                var (shouldExecute, timedOut) = ShouldExecuteEntry(entry);
                if (shouldExecute)
                {
                    ExecuteEntry(entry, timedOut);
                }
            }

            ExecutePendingAsyncActions();
        }
        catch (Exception e)
        {
            _telemetry.ErrorExecuteReadyWorkItemsFailed(exception: e);
        }

        IEnumerable<WorkItemEntry> GetReadyWorkItemQuery()
        {
            return State.Entries.TakeWhile(e => 
                e.NotEarlierThanUtc == null || 
                e.NotEarlierThanUtc.Value.Subtract(utcNow).TotalMilliseconds <= 50d ||
                e.NotLaterThanUtc <= utcNow);
        }

        void ExecuteEntry(WorkItemEntry entry, bool timedOut)
        {
            using var executeEntrySpan = _telemetry.SpanExecuteWorkItem(
                entry.TargetRef.GrainId, entry.WorkItem.GetType().Name, entry.Id, timedOut);
            
            try
            {
                Dispatch(new RemoveWorkItemEntryEvent(_environment.UtcNow, entry.Id));
                entry.TargetRef.Get().ExecuteWorkItem(entry.WorkItem, timedOut);
            }
            catch (Exception e)
            {
                executeEntrySpan.Fail(e);
            }
        }

        (bool shouldExecute, bool timedOut) ShouldExecuteEntry(WorkItemEntry entry)
        {
            var predicateResult = !entry.HasPredicate || EvaluatePredicate(entry);
            var timedOut = !predicateResult && entry.NotLaterThanUtc <= utcNow;
            var shouldExecute = predicateResult || timedOut;
            return (shouldExecute, timedOut);
        }

        void ExecutePendingAsyncActions()
        {
            while (_postedAsyncActions.Reader.TryRead(out var actionEntry))
            {
                using (_telemetry.SpanRunAsyncAction(key: actionEntry.Key))
                {
                    try
                    {
                        actionEntry.Action();
                    }
                    catch (Exception e)
                    {
                        _telemetry.ErrorFailedToExecuteAsyncAction(key: actionEntry.Key, exception: e);
                    }
                }
            }
        }
    }

    public DateTime NextWorkItemAtUtc => State.FirstWorkItemUtc;

    public IEnumerable<IGrainWorkItem> WorkItems => State.Entries.Select(e => e.WorkItem);

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        ImmutableSortedSet<WorkItemEntry> newEntries;

        switch (@event)
        {
            case InsertWorkItemEntryEvent insert:
                newEntries = stateBefore.Entries.Add(insert.Entry); 
                return stateBefore with {
                    Entries = newEntries,
                    FirstWorkItemUtc = GetFirstWorkItemUtc(newEntries, insert.UtcNow),
                    NextWorkItemId = insert.Entry.Id + 1
                }; 
            case RemoveWorkItemEntryEvent remove:
                var entry = stateBefore.Entries.First(e => e.Id == remove.EntryId); //TODO: optimize?
                newEntries = stateBefore.Entries.Remove(entry); 
                return stateBefore with {
                    Entries = newEntries,
                    FirstWorkItemUtc = GetFirstWorkItemUtc(newEntries, remove.UtcNow),
                }; 
            default:
                return stateBefore;
        }
    }

    private bool EvaluatePredicate(WorkItemEntry entry)
    {
        //TODO: telemetry create span
        try
        {
            return entry.TargetRef.Get().ShouldExecuteWorkItem(entry.WorkItem);
        }
        catch //(Exception e)
        {
            //TODO: telemetry log error
        }

        return false;
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<TaskQueueGrain, GrainActivationEvent>(
            TypeString, 
            (activation, context) => new TaskQueueGrain(
                grainId: activation.GrainId,
                grains: context.Resolve<ISiloGrains>(),
                dispatch: context.Resolve<ISiloEventDispatch>(),
                telemetry: context.Resolve<ISiloTelemetry>(),
                environment: context.Resolve<ISiloEnvironment>()));
    }

    private static DateTime GetFirstWorkItemUtc(ImmutableSortedSet<WorkItemEntry> items, DateTime utcNow)
    {
        if (items.IsEmpty)
        {
            return EmptyQueueFirstWorkItemUtc;
        }

        var firstItem = items.First();
        var result = firstItem.NotEarlierThanUtc ?? utcNow;
        return result;
    }
    
    public class WorkItemEntryComparer : IComparer<WorkItemEntry>
    {
        public int Compare(WorkItemEntry? x, WorkItemEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            var byUtc = Nullable.Compare(x.NotEarlierThanUtc, y.NotEarlierThanUtc);
            if (byUtc != 0)
            {
                return byUtc;
            }

            return x.Id < y.Id ? -1 : 1;
        }
    }

    private struct AsyncActionEntry
    {
        public readonly ulong Key;
        public readonly Action Action;

        public AsyncActionEntry(ulong key, Action action)
        {
            Key = key;
            Action = action;
        }
    }
}
