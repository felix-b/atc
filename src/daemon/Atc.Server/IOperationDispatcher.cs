namespace Atc.Server;

public interface IOperationDispatcher : IAsyncDisposable
{
    void DispatchOperation(IConnectionContext connection, object message);
}