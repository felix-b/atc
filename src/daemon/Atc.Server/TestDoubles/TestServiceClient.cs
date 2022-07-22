using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Atc.Server.TestDoubles;

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
            TestServiceClientAssert.Fail("TestServiceClient.TakeReceivedMessage: no more messages");
        }

        TestServiceClientAssert.IsTrue(_receivedEnvelopes.TryDequeue(out var envelope), "TestServiceClient.TakeReceivedMessage: failed");
        assertions(envelope!);
    }

    public async Task WaitForIncomingEnvelope(
        Func<TServerToClient, bool> predicate, 
        int millisecondsTimeout,
        string message = "")
    {
        var clock = Stopwatch.StartNew();

        while (!_receivedEnvelopes.Any(predicate))
        {
            if (clock.Elapsed.TotalMilliseconds >= millisecondsTimeout)
            {
                TestServiceClientAssert.Fail($"WaitForIncomingEnvelope ({message}) timed out after {millisecondsTimeout} ms");
            }

            await Task.Delay(200);
        }
    }

    public async Task WaitForIncomingEnvelopes(
        Func<TServerToClient, bool> predicate, 
        int count,
        int millisecondsTimeout,
        string message = "")
    {
        var clock = Stopwatch.StartNew();

        while (_receivedEnvelopes.Where(predicate).Count() < count)
        {
            if (clock.Elapsed.TotalMilliseconds >= millisecondsTimeout)
            {
                TestServiceClientAssert.Fail($"WaitForIncomingEnvelopes ({message}) timed out after {millisecondsTimeout} ms");
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
            TestServiceClientAssert.NotNull(deserialized, $"TestServiceClient.OnReceive: failed to deserialize incoming envelope");
            _receivedEnvelopes.Enqueue(deserialized);
        }
        catch (Exception e)
        {
            TestServiceClientAssert.Fail($"TestServiceClient.OnReceive: failed to deserialize incoming envelope: {e}");
        }

        return Task.CompletedTask;
    }
}

public static class TestServiceClientAssert
{
    public delegate void AssertFailMethod(string message);
    public delegate void AssertNotNullMethod(object?  obj, string? message = null, params object[] args);
    public delegate void AssertIsTrueMethod(bool value, string? message = null, params object[] args);
    
    public static AssertFailMethod Fail = msg => throw new NotImplementedException();
    public static AssertNotNullMethod NotNull = (obj, msg, args) => throw new NotImplementedException();
    public static AssertIsTrueMethod IsTrue = (value, msg, args) => throw new NotImplementedException();
}
