using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Atc.Utilities;

namespace Atc.Server;

public class WebSocketClientChannel : IAsyncDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly string _url;
    private readonly ReceiveHandler _onReceive;
    private readonly IClientChannelTelemetry _telemetry;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly WriteLocked<ImmutableHashSet<Task>> _sendTasks = new(ImmutableHashSet<Task>.Empty);
    private readonly Task _connectTask;
    private readonly Task<SocketReceiveStatus>? _receiveTask = null;

    public WebSocketClientChannel(string url, ReceiveHandler onReceive, IClientChannelTelemetry telemetry)
    {
        _url = url;
        _onReceive = onReceive;
        _telemetry = telemetry;
        _webSocket = new ClientWebSocket();
        _connectTask = Connect(); 
        _receiveTask = Receive();
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        
        await _connectTask;

        var sendTasksSnapshot = _sendTasks.Read();
        foreach (var sendTask in sendTasksSnapshot)
        {
            await sendTask;
        }

        if (_receiveTask != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                _telemetry.DebugClientSocketInitiatingCloseHandshake();
                await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session over", CancellationToken.None);
            }

            _telemetry.DebugClientJoiningReceiveLoop();

            var receiveLoopStatus = await _receiveTask;
            _telemetry.DebugClientReceiveLoopExited(receiveLoopStatus);
        }

        using (_telemetry.SpanClientSocketDisposing())
        {
            _webSocket.Dispose();
        }
        
        _telemetry.InfoClientSocketDisconnected();
    }

    public async Task Send(ReadOnlyMemory<byte> messageBytes)
    {
        await _connectTask;
        _cancellation.Token.ThrowIfCancellationRequested();
        
        var task = SendOnSocket();
        _sendTasks.Replace(hashSet => hashSet.Add(task));

        try
        {
            await task;
        }
        finally
        {
            _sendTasks.Replace(hashSet => hashSet.Remove(task));
        }

        async Task SendOnSocket()
        { 
            await Task.Yield();
            await _webSocket.SendAsync(messageBytes, WebSocketMessageType.Binary, true, _cancellation.Token);
        }
    }

    private async Task Connect()
    {
        _telemetry.DebugClientSocketConnecting(url: _url);
        await _webSocket.ConnectAsync(new Uri(_url), _cancellation.Token);        
        _telemetry.InfoClientSocketConnected(url: _url);
    }

    private async Task<SocketReceiveStatus> Receive()
    {
        _telemetry.DebugClientReceiveLoopWaitingForConnect();
        await _connectTask;
        _telemetry.DebugClientReceiveLoopStarting();

        var incomingMessageBuffer = new byte[8192];

        while (!_cancellation.IsCancellationRequested)
        {
            var (bytesReceived, status) = await ReadOneMessage(incomingMessageBuffer);
            if (bytesReceived < 0 || status != SocketReceiveStatus.MessageReceived)
            {
                if (status == SocketReceiveStatus.SocketClosing && _webSocket.State != WebSocketState.Closed)
                {
                    _telemetry.DebugClientReplyingSocketCloseHandshake();
                    await _webSocket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Server closed session", 
                        CancellationToken.None);
                }
                return status;
            }

            var bufferSegment = new ArraySegment<byte>(incomingMessageBuffer, 0, bytesReceived);
            await _onReceive(bufferSegment);
        }

        return SocketReceiveStatus.ConnectionCanceled;
    }

    private async ValueTask<(int bytesReceived, SocketReceiveStatus status)> ReadOneMessage(byte[] incomingMessageBuffer)
    {
        int bytesReceived = 0;
                
        while (!_cancellation.IsCancellationRequested)
        {
            var bufferSegment = new ArraySegment<byte>(
                incomingMessageBuffer, 
                offset: bytesReceived, 
                count: incomingMessageBuffer.Length - bytesReceived);

            try
            {
                var receiveResult = await _webSocket.ReceiveAsync(bufferSegment, CancellationToken.None);
                bytesReceived += receiveResult.Count;
                _telemetry.DebugClientReceivedFrame(bytesReceived: receiveResult.Count, receiveResult.MessageType);

                if (receiveResult.MessageType != WebSocketMessageType.Binary)
                {
                    return (-1, receiveResult.MessageType == WebSocketMessageType.Close
                        ? SocketReceiveStatus.SocketClosing
                        : SocketReceiveStatus.ProtocolError);
                }

                if (receiveResult.EndOfMessage)
                {
                    _telemetry.DebugClientReceivedMessage(sizeBytes: bytesReceived);
                    return (bytesReceived, SocketReceiveStatus.MessageReceived);
                }
            }
            catch (OperationCanceledException)
            {
                _telemetry.DebugClientReceiveLoopCancelled();
            }
            catch (WebSocketException e)
            {
                _telemetry.ErrorClientSocketError(code: e.WebSocketErrorCode, exception: e);
                return (-1, SocketReceiveStatus.ProtocolError);
            }
        }

        return (-1, SocketReceiveStatus.ConnectionCanceled);
    }

    public delegate Task ReceiveHandler(ArraySegment<byte> messageBytes);
}
