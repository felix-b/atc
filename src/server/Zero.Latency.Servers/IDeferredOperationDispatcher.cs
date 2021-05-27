using System;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut> : IAsyncDisposable
        where TEnvelopeIn : class
        where TEnvelopeOut : class
    {
        void DispatchOperation(IDeferredConnectionContext<TEnvelopeOut> connection, TEnvelopeIn envelope);
    }
}