using System;
using System.Collections.Immutable;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Just.Utility;
using Microsoft.AspNetCore.Http;
using Zero.Doubt.Logging;

namespace Zero.Latency.Servers
{
    public class SocketConnectionManager : ISocketAcceptor, IServiceHostContext
    {
        private readonly IMessageSerializer _serializer;
        private readonly IOperationDispatcher _dispatcher;
        private readonly IEndpointLogger _logger;
        private WriteLocked<ImmutableList<Connection>> _connections = ImmutableList<Connection>.Empty;
        private long _nextConnectionId = 1;
        private bool _disposed = false;

        public SocketConnectionManager(IMessageSerializer serializer, IOperationDispatcher dispatcher, IEndpointLogger logger)
        {
            _serializer = serializer;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await _connections.DisposeAllAsync();
                await _dispatcher.DisposeAsync();
            }
        }

        public Task AcceptSocket(HttpContext context, WebSocket socket, CancellationToken cancel)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"SocketConnectionManager was disposed.");
            }

            var connectionId = Interlocked.Increment(ref _nextConnectionId);
            LogEngine.BranchAsyncTask($"accept-socket #{connectionId}");
                
            var connection = new Connection(this, connectionId, socket, cancel);
            _connections.Replace(list => list.Add(connection));
            return connection.RunReceiveLoop();
        }
        
        ValueTask IServiceHostContext.CloseConnection(Connection connection)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }
            
            _connections.Replace(list => list.Remove(connection));
            return connection.DisposeAsync();
        }

        IMessageSerializer IServiceHostContext.Serializer => _serializer;

        IOperationDispatcher IServiceHostContext.Dispatcher => _dispatcher;

        IEndpointLogger IServiceHostContext.Logger => _logger;
    }
}
