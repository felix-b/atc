using System;
using System.Collections.Immutable;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Zero.Latency.Servers
{
    public class SocketConnectionManager : ISocketAcceptor, IServiceHostContext
    {
        private readonly IMessageSerializer _serializer;
        private readonly IOperationDispatcher<object, object> _dispatcher;
        private WriteLocked<ImmutableList<Connection>> _connections = ImmutableList<Connection>.Empty;
        private long _nextConnectionId = 1;
        private bool _disposed = false;

        public SocketConnectionManager(IMessageSerializer serializer, IOperationDispatcher<object, object> dispatcher)
        {
            _serializer = serializer;
            _dispatcher = dispatcher;
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
            
            var connection = new Connection(this, Interlocked.Increment(ref _nextConnectionId), socket, cancel);
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

        IOperationDispatcher<object, object> IServiceHostContext.Dispatcher => _dispatcher;
    }
}