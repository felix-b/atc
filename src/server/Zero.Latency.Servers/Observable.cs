using System;
using System.Collections.Generic;

namespace Zero.Latency.Servers
{
    public interface IObservable<out T>
    {
        IObserverSubscription Subscribe(Action<T> observer);
        T LastKnown { get; }
    }

    public interface IObservableQuery<out T>
    {
        IObserverSubscription Subscribe(QueryObserver<T> observer, QueryConsumer<T>? consumeCurrentResults = null);
    }

    public delegate void QueryObserver<in T>(
        IEnumerable<T> added,
        IEnumerable<T> removed,
        IEnumerator<T> updated);

    public delegate void QueryConsumer<in T>(IEnumerable<T> results);

    public interface IObserverSubscription : IAsyncDisposable
    {
    }
}
