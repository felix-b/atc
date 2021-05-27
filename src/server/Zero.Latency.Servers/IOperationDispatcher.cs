using System;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IOperationDispatcher : IAsyncDisposable
    {
        void DispatchOperation(IConnectionContext connection, object message);
    }
}
