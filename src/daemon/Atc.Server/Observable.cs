namespace Atc.Server;

public interface IObservable<out T>
{
    IObserverSubscription Subscribe(Action<T> observer);
    T LastKnown { get; }
}

public interface IObservableQuery<T>
{
    IObserverSubscription Subscribe(QueryObserver<T> observer);
    IEnumerable<T> GetResults();
}

public readonly struct QueryObservation<T>
{
    public QueryObservation(IEnumerable<T> added, IEnumerable<T> updated, IEnumerable<T> removed)
    {
        Added = added;
        Updated = updated;
        Removed = removed;
    }
    
    public readonly IEnumerable<T> Added;
    public readonly IEnumerable<T> Updated;
    public readonly IEnumerable<T> Removed;
}
    
public delegate void QueryObserver<T>(in QueryObservation<T> observation);

public interface IObserverSubscription : IAsyncDisposable
{
}