using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers.Tests
{
    public class TestConnectionContext<TEnvelopeOut> : IDeferredConnectionContext<TEnvelopeOut>
        where TEnvelopeOut : class
    {
        public void FireMessage(TEnvelopeOut message)
        {
            OnFireMessage?.Invoke(message);
        }

        public void RegisterObserver(IObserverSubscription observer)
        {
            OnRegisterObserver?.Invoke(observer);
        }

        public void RequestClose()
        {
            OnRequestClose?.Invoke();
        }

        public void RequestFlush()
        {
            OnRequestFlush?.Invoke();
        }

        public long Id { get; set; } = 123;
        public bool IsActive { get; set; } = true;
        public CancellationToken Cancellation { get; set; } = CancellationToken.None;
        public SessionItems Session { get; } = new(initialEntryCount: 4);
        public Action<TEnvelopeOut>? OnFireMessage { get; set; }
        public Action<IObserverSubscription>? OnRegisterObserver { get; set; }
        public Action? OnRequestClose { get; set; }
        public Action? OnRequestFlush { get; set; }
    }
}
