using System;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IOperationDispatcher<TIncomingMessage, TOutgoingMessage> : IAsyncDisposable
        where TIncomingMessage : class
        where TOutgoingMessage : class
    {
        ValueTask DispatchOperation(IConnectionContext<TOutgoingMessage> connection, TIncomingMessage message);
    }
}
