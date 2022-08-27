using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using GeneratedCode;

namespace Atc.Server;

public class WebSocketServiceClient<TClientToServer, TServerToClient> : IAsyncDisposable
    where TClientToServer : class
    where TServerToClient : class
{
    private readonly WebSocketClientChannel _channel;
    private readonly ConcurrentQueue<TServerToClient> _receivedEnvelopes = new();

    public WebSocketServiceClient(string url, IClientChannelTelemetry? telemetry = null)
    {
        var effectiveTelemetry = telemetry ?? AtcServerTelemetry.CreateNoopTelemetry<IClientChannelTelemetry>(); 

        _channel = new WebSocketClientChannel(
            url, 
            OnReceive, 
            effectiveTelemetry);
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

    public async Task<TServerToClient?> WaitForIncomingEnvelope(
        Func<TServerToClient, bool> predicate, 
        int millisecondsTimeout)
    {
        var clock = Stopwatch.StartNew();

        while (true)
        {
            var envelope = _receivedEnvelopes.FirstOrDefault(predicate);
            if (envelope != null)
            {
                return envelope;
            }
            
            if (clock.Elapsed.TotalMilliseconds >= millisecondsTimeout)
            {
                return null;
            }

            await Task.Delay(100);
        }
    }

    public async Task<TServerToClient[]> WaitForIncomingEnvelopes(
        Func<TServerToClient, bool> predicate, 
        int count,
        int millisecondsTimeout)
    {
        var clock = Stopwatch.StartNew();

        while (true)
        {
            var actualCount = _receivedEnvelopes.Where(predicate).Count();
            
            if (clock.Elapsed.TotalMilliseconds >= millisecondsTimeout || actualCount >= count)
            {
                return _receivedEnvelopes.Where(predicate).ToArray();
            }

            await Task.Delay(200);
        }
    }

    public IEnumerable<TServerToClient> ReceivedEnvelopes => _receivedEnvelopes;

    private Task OnReceive(ArraySegment<byte> messageBytes)
    {
        try
        {
            var deserialized = ProtoBuf.Serializer.Deserialize<TServerToClient>(messageBytes.AsSpan());
            Console.Error.WriteLine($"WebSocketServiceClient.OnReceive: failed to deserialize incoming envelope (null)");
            _receivedEnvelopes.Enqueue(deserialized);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"WebSocketServiceClient.OnReceive: failed to deserialize incoming envelope: ${e}");
        }

        return Task.CompletedTask;
    }
}
