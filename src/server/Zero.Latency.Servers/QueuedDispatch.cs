#if false

using System.Collections.Concurrent;
using System.Threading;

namespace Zero.Latency.Servers
{
    public class QueuedDispatch : IServiceDispatch
    {
        private readonly IServiceDispatch _next;
        private readonly BlockingCollection<WorkItem> _workerItems;
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _cancellation = new();

        public QueuedDispatch(IServiceDispatch next)
        {
            _next = next;

            // create invokers for service instance
            // spin up worker thread
        }

        public void Dispose()
        {
            // raise cancellation flag
            // join worker thread
        }
        
        public void DispatchOperation(IConnectionContext connection, ClientToServer message)
        {
            // add to worker queue
            throw new System.NotImplementedException();
        }

        private record WorkItem(
            IConnectionContext Connection, 
            ClientToServer Message
        );

        private class ProxyConnectionContext : IConnectionContext
        {
            private readonly IConnectionContext _inner;
            
            
            void IConnectionContext.FireMessage(ServerToClient message)
            {
                throw new System.NotImplementedException();
            }

            void IConnectionContext.RegisterObserver(IObserverSubscription observer)
            {
                throw new System.NotImplementedException();
            }

            void IConnectionContext.CloseConnection()
            {
                throw new System.NotImplementedException();
            }

            int IConnectionContext.Id => _inner.Id;

            bool IConnectionContext.IsActive => _inner.IsActive;

            CancellationToken IConnectionContext.Cancellation => _inner.Cancellation;
        }
    }
}

#endif