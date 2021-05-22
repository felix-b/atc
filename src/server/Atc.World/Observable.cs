using System;

namespace Atc.World
{
    public interface IObservable<out TResult>
    {
        TResult LastKnown { get; }
        TResult Subscribe(Action<TResult> callback);
        void Unsubscribe(Action<TResult> callback);
    }

    public interface IObservableQuery<TQuery, out TResult>
    {
        IObservable<TResult> Observe(in TQuery query);
    }
}
