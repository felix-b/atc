using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IReadOnlyConnectionContext
    {
        long Id { get; }
        bool IsActive { get; }
        CancellationToken Cancellation { get; }
        
        //TODO: add more info about the client/connection?
    }

    public interface IConnectionContext : IReadOnlyConnectionContext
    {
        ValueTask SendMessage(object outgoingMessageEnvelope);
        void RegisterObserver(IObserverSubscription observer);
        ValueTask CloseConnection();
        SessionItems Session { get; }
    }
}
