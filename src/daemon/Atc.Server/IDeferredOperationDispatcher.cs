namespace Atc.Server;

public interface IDeferredOperationDispatcher<TEnvelopeIn, TEnvelopeOut> : IAsyncDisposable
    where TEnvelopeIn : class
    where TEnvelopeOut : class
{
    void DispatchOperation(IDeferredConnectionContext<TEnvelopeOut> connection, TEnvelopeIn envelope);
}