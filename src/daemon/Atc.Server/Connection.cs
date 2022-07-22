using System.Buffers;
using System.Collections.Immutable;
using System.Net.WebSockets;

namespace Atc.Server;

public class Connection : IAsyncDisposable, IConnectionContext
{
    private readonly IServiceHostContext _serviceHost;
    private readonly long _id;
    private readonly WebSocket _socket;
    private readonly CancellationToken _cancelByHost;
    private readonly CancellationTokenSource _cancelForAnyReason;
    private readonly ArrayBufferWriter<byte> _outgoingMessageBuffer = new(initialCapacity: 16384); //TODO: how to limit max buffer size?
    private readonly IEndpointTelemetry _telemetry;
    private SessionItems _sessionItems = new(initialEntryCount: 4);
    private WriteLocked<ImmutableList<ObserverEntry>> _observers = ImmutableList<ObserverEntry>.Empty;
    private Task? _receiveLoopTask = null;
    private bool _disposed = false;

    public Connection(
        IServiceHostContext serviceHost,
        IEndpointTelemetry telemetry,
        long id, 
        WebSocket socket, 
        CancellationToken cancelByHost)
    {
        _serviceHost = serviceHost;
        _telemetry = telemetry;
        _id = id;
        _socket = socket;
        _cancelByHost = cancelByHost;
        _cancelForAnyReason = CancellationTokenSource.CreateLinkedTokenSource(cancelByHost);

        _telemetry.InfoConnectionSocketOpened(connectionId: _id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            _telemetry.VerboseConnectionAlreadyDisposedIgnoring(connectionId: _id);
            return;
        }

        using var traceSpan = _telemetry.SpanConnectionDispose(connectionId: _id, socketState: _socket.State);

        try
        {
            _disposed = true;
            _cancelForAnyReason.Cancel();

            _telemetry.DebugConnectionDisposingObservers(connectionId: _id);
            await _observers.DisposeAllAsync();

            if (_socket.State == WebSocketState.Open)
            {
                _telemetry.DebugConnectionInitiatingSocketCloseHandshake(connectionId: _id);
                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session over", CancellationToken.None);
            }

            if (_receiveLoopTask != null)
            {
                _telemetry.DebugConnectionJoiningReceiveLoop(connectionId: _id);
                await _receiveLoopTask;
            }
                
            _telemetry.DebugConnectionSocketDisposing(connectionId: _id, socketState: _socket.State);
            _socket.Dispose();
            _telemetry.InfoConnectionSocketClosed(connectionId: _id);
        }
        catch (Exception e)
        {
            traceSpan.Fail(e);
        }
    }

    public Task RunReceiveLoop()
    {
        using var traceSpan = _telemetry.SpanConnectionRunReceiveLoop(connectionId: _id);
            
        ValidateActive();

        if (_receiveLoopTask != null)
        {
            throw new InvalidOperationException($"Connection id [{_id}]: receive loop already started");
        }
            
        _receiveLoopTask = PrivateRunReceiveLoop();
        return _receiveLoopTask;
    }
        
    public ValueTask CloseConnection()
    {
        return DisposeAsync();
    }

    public ValueTask SendMessage(object message)
    {
        ValidateActive();

        if (_socket.State != WebSocketState.Open)
        {
            //TODO: log to telemetry; dispose connection?
            return ValueTask.CompletedTask;
        }

        _outgoingMessageBuffer.Clear();
        _serviceHost.Serializer.SerializeOutgoingEnvelope(message, _outgoingMessageBuffer);
        
        return _socket.SendAsync(
            _outgoingMessageBuffer.WrittenMemory, 
            WebSocketMessageType.Binary, 
            endOfMessage: true, 
            _cancelForAnyReason.Token);
    }

    public void RegisterObserver(IObserverSubscription observer, string? registrationKey)
    {
        ValidateActive();
        _observers.Replace(list => list.Add(new ObserverEntry(observer, registrationKey)));
    }

    public async ValueTask DisposeObserver(string registrationKey)
    {
        ValidateActive();
        IObserverSubscription? foundSubscription = null;
            
        _observers.Replace(list => {
            var index = list.FindIndex(entry => entry.RegistrationKey == registrationKey);
            if (index >= 0)
            {
                foundSubscription = list[index].Subscription;
                return list.RemoveAt(index);
            }
            return list;
        });

        if (foundSubscription != null)
        {
            await foundSubscription.DisposeAsync();
        }
    }

    public long Id => _id;

    public bool IsActive =>
        !_disposed &&
        !_cancelForAnyReason.IsCancellationRequested; 
        //TODO: && _socket.State == WebSocketState.Open;

    // service operations use this token as abort processing of a request
    public CancellationToken Cancellation => _cancelForAnyReason.Token;

    public SessionItems Session => _sessionItems;

    private void ValidateActive()
    {
        if (!IsActive)
        {
            throw new ObjectDisposedException($"Connection id [{_id}] was disposed");
        }
    }

    private async Task PrivateRunReceiveLoop()
    {
        var receiveStatus = SocketReceiveStatus.Unknown;

        try
        {
            receiveStatus = await RunLoop();
            _telemetry.DebugConnectionReceiveLoopCompleted(connectionId: _id, receiveStatus);

            if (receiveStatus == SocketReceiveStatus.SocketClosing && _socket.State != WebSocketState.Closed)
            {
                _telemetry.DebugConnectionSocketReplyingCloseHandshake(connectionId: _id, socketState: _socket.State);
                var closeStatus = WebSocketCloseStatus.NormalClosure;
                await _socket.CloseOutputAsync(closeStatus, closeStatus.ToString(), CancellationToken.None);
            }

            // var closeStatus = receiveStatus == SocketReceiveStatus.ConnectionCanceled
            //     ? WebSocketCloseStatus.EndpointUnavailable
            //     : WebSocketCloseStatus.ProtocolError;
            // await _socket.CloseAsync(closeStatus, closeStatus.ToString(), CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _telemetry.DebugConnectionReceiveLoopCanceled(connectionId: _id);
        }
        catch (WebSocketException e)
        {
            receiveStatus = SocketReceiveStatus.SocketClosing;
            _telemetry.ErrorSocketReceiveLoopFailure(connectionId: _id, socketErrorCode: e.WebSocketErrorCode, e);
        }
        catch (Exception e)
        {
            receiveStatus = SocketReceiveStatus.SocketClosing;
            _telemetry.ErrorSocketReceiveLoopFailure(connectionId: _id, socketErrorCode: null, e);
        }
        finally
        {
            await _serviceHost.RemoveClosedConnection(this);
        }
            
        async ValueTask<SocketReceiveStatus> RunLoop()
        {
            var incomingMessageBuffer = new byte[8192];
            var serializer = _serviceHost.Serializer;
            var dispatcher = _serviceHost.Dispatcher;

            while (!_cancelForAnyReason.IsCancellationRequested)
            {
                var (bytesReceived, status) = await ReadOneMessage(incomingMessageBuffer);
                if (bytesReceived < 0 || status != SocketReceiveStatus.MessageReceived)
                {
                    return status;
                }

                var bufferSegment = new ArraySegment<byte>(incomingMessageBuffer, 0, bytesReceived);
                var message = serializer.DeserializeIncomingEnvelope(bufferSegment);
                dispatcher.DispatchOperation(this, message);
            }

            return SocketReceiveStatus.ConnectionCanceled;
        }

        async ValueTask<(int bytesReceived, SocketReceiveStatus status)> ReadOneMessage(byte[] incomingMessageBuffer)
        {
            int bytesReceived = 0;
                
            while (!_cancelForAnyReason.IsCancellationRequested)
            {
                var bufferSegment = new ArraySegment<byte>(
                    incomingMessageBuffer, 
                    offset: bytesReceived, 
                    count: incomingMessageBuffer.Length - bytesReceived);

                var receiveResult = await _socket.ReceiveAsync(bufferSegment, CancellationToken.None);
                bytesReceived += receiveResult.Count;

                if (receiveResult.MessageType != WebSocketMessageType.Binary)
                {
                    return (-1, receiveResult.MessageType == WebSocketMessageType.Close
                        ? SocketReceiveStatus.SocketClosing
                        : SocketReceiveStatus.ProtocolError);
                }
                if (receiveResult.EndOfMessage)
                {
                    _telemetry.DebugConnectionMessageReceived(connectionId: _id, sizeBytes: bytesReceived);
                    return (bytesReceived, SocketReceiveStatus.MessageReceived);
                }
            }

            return (-1, SocketReceiveStatus.ConnectionCanceled);
        }
    }

    private partial record ObserverEntry(IObserverSubscription Subscription, string? RegistrationKey);

    private partial record ObserverEntry : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return Subscription.DisposeAsync();
        }
    }
}