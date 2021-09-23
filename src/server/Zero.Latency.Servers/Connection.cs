using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Zero.Doubt.Logging;

namespace Zero.Latency.Servers
{
    public class Connection : IAsyncDisposable, IConnectionContext
    {
        private readonly IServiceHostContext _serviceHost;
        private readonly long _id;
        private readonly WebSocket _socket;
        private readonly CancellationToken _cancelByHost;
        private readonly CancellationTokenSource _cancelForAnyReason;
        private readonly ArrayBufferWriter<byte> _outgoingMessageBuffer = new(initialCapacity: 16384); //TODO: how to limit max buffer size?
        private SessionItems _sessionItems = new(initialEntryCount: 4);
        private WriteLocked<ImmutableList<ObserverEntry>> _observers = ImmutableList<ObserverEntry>.Empty;
        private Task? _receiveLoopTask = null;
        private bool _disposed = false;

        public Connection(
            IServiceHostContext serviceHost,
            long id, 
            WebSocket socket, 
            CancellationToken cancelByHost)
        {
            _serviceHost = serviceHost;
            _id = id;
            _socket = socket;
            _cancelByHost = cancelByHost;
            _cancelForAnyReason = CancellationTokenSource.CreateLinkedTokenSource(cancelByHost);

            Console.WriteLine($">>>--->>-- SOCKET OPEN: client connection id[{_id}] --<<---<<<");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancelForAnyReason.Cancel();

            await _observers.DisposeAllAsync();
            if (_receiveLoopTask != null)
            {
                await _receiveLoopTask;
            }
            
            _socket.Dispose();
            Console.WriteLine($">>>--->>-- SOCKET CLOSE: client connection id[{_id}] --<<---<<<");
        }

        public Task RunReceiveLoop()
        {
            LogEngine.BranchAsyncTask($"socket-receive-loop #{_id}");
            
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

        public bool IsActive => !_disposed && !_cancelForAnyReason.IsCancellationRequested;

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
            LogEngine.BranchAsyncTask($"socket-receive-loop #{_id}");
            var receiveStatus = SocketReceiveStatus.Unknown;

            try
            {
                receiveStatus = await RunLoop();

                var closeStatus = receiveStatus == SocketReceiveStatus.ConnectionCanceled
                    ? WebSocketCloseStatus.EndpointUnavailable
                    : WebSocketCloseStatus.ProtocolError;
                await _socket.CloseAsync(closeStatus, closeStatus.ToString(), CancellationToken.None);
            }
            catch (WebSocketException e)
            {
                receiveStatus = SocketReceiveStatus.SocketClosing;
                Console.WriteLine($"SOCKET RECEIVE LOOP FAILURE! Connection id[{_id}]: Error {e.WebSocketErrorCode}: {e.Message}");
            }
            catch (Exception e)
            {
                receiveStatus = SocketReceiveStatus.SocketClosing;
                Console.WriteLine($"SOCKET RECEIVE LOOP FAILURE! Connection id[{_id}]: {e.Message}");
            }
            finally
            {
                await _serviceHost.CloseConnection(this);
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
                    
                    var receiveResult = await _socket.ReceiveAsync(bufferSegment, _cancelForAnyReason.Token);
                    bytesReceived += receiveResult.Count;

                    if (receiveResult.MessageType != WebSocketMessageType.Binary)
                    {
                        return (-1, receiveResult.MessageType == WebSocketMessageType.Close
                            ? SocketReceiveStatus.SocketClosing
                            : SocketReceiveStatus.ProtocolError);
                    }
                    if (receiveResult.EndOfMessage)
                    {
                        return (bytesReceived, SocketReceiveStatus.MessageReceived);
                    }
                }

                return (-1, SocketReceiveStatus.ConnectionCanceled);
            }
        }

        private enum SocketReceiveStatus
        {
            Unknown,
            MessageReceived,
            ProtocolError,
            SocketClosing,
            ConnectionCanceled
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
}
