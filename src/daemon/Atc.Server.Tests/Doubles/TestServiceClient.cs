#if false

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Atc.Server.Tests.Doubles;

public class TestServiceClient<TClientToServer, TServerToClient> : IAsyncDisposable
    where TClientToServer : class
    where TServerToClient : class 
{
    private readonly TestClientChannelTelemetry _telemetry = new();
    private readonly WebSocketClientChannel _channel;
    private readonly ConcurrentQueue<TServerToClient> _receivedEnvelopes = new();

    public TestServiceClient(string url)
    {
        _channel = new WebSocketClientChannel(url, OnReceive, _telemetry);
    }

    public ValueTask DisposeAsync()
    {
        return _channel.DisposeAsync();
    }

    public Task SendEnvelope(TClientToServer envelope)
    {
        ArrayBufferWriter<byte> outgoingMessageBuffer = new(initialCapacity: 4096); 
        ProtoBuf.Serializer.Serialize(outgoingMessageBuffer, envelope);
        return _channel.Send(outgoingMessageBuffer.WrittenMemory);
    }

    public void TakeReceivedEnvelope(Action<TServerToClient> assertions)
    {
        if (_receivedEnvelopes.IsEmpty)
        {
            Assert.Fail("TestServiceClient.TakeReceivedMessage: no more messages");
        }

        Assert.IsTrue(_receivedEnvelopes.TryDequeue(out var envelope), "TestServiceClient.TakeReceivedMessage: failed");
        assertions(envelope!);
    }

    public async Task WaitForIncomingEnvelope(Func<TServerToClient, bool> predicate, int milliseconds, string message = "")
    {
        var clock = Stopwatch.StartNew();
        
        while (!_receivedEnvelopes.Any(predicate))
        {
            if (clock.Elapsed.TotalMilliseconds >= milliseconds)
            {
                Assert.Fail($"WaitForIncomingEnvelope ({message}) timed out after {milliseconds} ms");
            }
            
            await Task.Delay(200);
        }
    }

    public IEnumerable<TServerToClient> ReceivedEnvelopes => _receivedEnvelopes;

    public TestClientChannelTelemetry Telemetry => _telemetry;

    private Task OnReceive(ArraySegment<byte> messageBytes)
    {
        try
        {
            var deserialized = ProtoBuf.Serializer.Deserialize<TServerToClient>(messageBytes.AsSpan());
            Assert.NotNull(deserialized, $"TestServiceClient.OnReceive: failed to deserialize incoming envelope");
            _receivedEnvelopes.Enqueue(deserialized);
        }
        catch (Exception e)
        {
            Assert.Fail($"TestServiceClient.OnReceive: failed to deserialize incoming envelope: {e}");
        }

        return Task.CompletedTask;
    }
}

#endif