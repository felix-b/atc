using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers.Tests
{
    public class TestConnectionContext : IConnectionContext<object>
    {
        public ValueTask SendMessage(object message)
        {
            OnSendMessage?.Invoke(message);
            return ValueTask.CompletedTask;
        }

        public void RegisterObserver(IObserverSubscription observer)
        {
            OnRegisterObserver?.Invoke(observer);
        }

        public ValueTask CloseConnection()
        {
            OnCloseConnection?.Invoke();
            return ValueTask.CompletedTask;
        }

        public long Id { get; set; } = 123;
        public bool IsActive { get; set; } = true;
        public CancellationToken Cancellation { get; set; } = CancellationToken.None;
        public Action<object>? OnSendMessage { get; set; }
        public Action<IObserverSubscription>? OnRegisterObserver { get; set; }
        public Action? OnCloseConnection { get; set; }
    }
}