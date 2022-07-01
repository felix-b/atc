using System.Collections.Immutable;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Atc.Server;

public class SocketConnectionManager : ISocketAcceptor, IServiceHostContext
{
    private readonly IMessageSerializer _serializer;
    private readonly IOperationDispatcher _dispatcher;
    private readonly IEndpointTelemetry _telemetry;
    private WriteLocked<ImmutableList<Connection>> _connections = ImmutableList<Connection>.Empty;
    private long _nextConnectionId = 1;
    private bool _disposed = false;

    public SocketConnectionManager(IMessageSerializer serializer, IOperationDispatcher dispatcher, IEndpointTelemetry telemetry)
    {
        _serializer = serializer;
        _dispatcher = dispatcher;
        _telemetry = telemetry;
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
        using var span = _telemetry.SpanAcceptingSocket(connectionId);
                
        var connection = new Connection(this, _telemetry, connectionId, socket, cancel);
        _connections.Replace(list => list.Add(connection));
        return connection.RunReceiveLoop();
    }
        
    ValueTask IServiceHostContext.RemoveClosedConnection(Connection connection)
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }
            
        _connections.Replace(list => list.Remove(connection));
        return ValueTask.CompletedTask;
    }

    IMessageSerializer IServiceHostContext.Serializer => _serializer;
    IOperationDispatcher IServiceHostContext.Dispatcher => _dispatcher;

    IEndpointTelemetry IServiceHostContext.Telemetry => _telemetry;
}