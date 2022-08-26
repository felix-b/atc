using System.Collections.Immutable;
using System.Threading.Channels;
using Atc.Server;
using Atc.Telemetry.CodePath;
using Atc.Utilities;
using GeneratedCode;

namespace Atc.Telemetry.Exporters.CodePath;

public class CodePathWebSocketExporter : ICodePathExporter, IDisposable
{
    private readonly TimeSpan? _delayBeforeFirstPush;
    private readonly Channel<MemoryStream> _buffers;
    private readonly ChannelWriter<MemoryStream> _writer;
    private readonly ChannelReader<MemoryStream> _reader;
    private readonly CancellationTokenSource _cancellation;
    private readonly CodePathExporterService _service;    
    private readonly WebSocketEndpoint _endpoint;
    private readonly WriteLocked<ImmutableList<ICodePathBuffersObserver>> _subscribedObservers;
    private readonly Task _buffersPumpingTask;
    private ICodePathEnvironment? _environment = null;
    private ulong _totalBufferCount = 0;
    private ulong _totalBroadcastCount = 0;
    private ulong _totalSendBufferCount = 0;

    public CodePathWebSocketExporter(
        int listenPortNumber, 
        IEndpointTelemetry? telemetry = null, 
        TimeSpan? delayBeforeFirstPush = null)
    {
        var effectiveTelemetry = telemetry ?? AtcServerTelemetry.CreateNoopTelemetry<IEndpointTelemetry>();
        _delayBeforeFirstPush = delayBeforeFirstPush;
        
        _buffers = Channel.CreateBounded<MemoryStream>(capacity: 100000);
        _reader = _buffers.Reader;
        _writer = _buffers.Writer;
        _subscribedObservers = new(ImmutableList<ICodePathBuffersObserver>.Empty);
        _cancellation = new CancellationTokenSource();
        _service = new CodePathExporterService(this);
        _endpoint = CreateServiceEndpoint(listenPortNumber, effectiveTelemetry);
        _endpoint.StartAsync().Wait();
        _buffersPumpingTask = RunBuffersPumpingLoop();
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _endpoint.StopAsync(TimeSpan.FromSeconds(5)).Wait(1000);
        _endpoint.DisposeAsync().AsTask().Wait(1000);
        _buffersPumpingTask.Wait(1000);
    }

    public void InjectEnvironment(ICodePathEnvironment environment)
    {
        _environment = environment;
    }

    public void PushBuffer(MemoryStream buffer)
    {
        Interlocked.Increment(ref _totalBufferCount);
        _writer.TryWrite(buffer);
    }
    
    public void SubscribeObserver(ICodePathBuffersObserver observer)
    {
        _subscribedObservers.Replace(list => list.Contains(observer)
            ? list
            : list.Add(observer));
    }

    public void UnsubscribeObserver(ICodePathBuffersObserver observer)
    {
        _subscribedObservers.Replace(list => list.Remove(observer));
    }

    public CodePathExporterService Service => _service;
    public ICodePathEnvironment? Environment => _environment;
    public CodePathStringMap? StringMap => _environment?.GetStringMap();
    public ulong TotalBufferCount => _totalBufferCount;
    public ulong TotalBroadcastCount => _totalBroadcastCount;
    public ulong TotalSendBufferCount => _totalSendBufferCount;
    public ulong TotalFireMessageCount => _service.TotalFireMessageCount;
    public ulong TotalObserveBuffersCount => _service.TotalObserveBuffersCount;
    public ulong TotalTelemetryBytes => _service.TotalTelemetryBytes;

    private WebSocketEndpoint CreateServiceEndpoint(int listenPortNumber, IEndpointTelemetry telemetry)
    {
        var endpoint = WebSocketEndpoint
            .Define()
            .ReceiveMessagesOfType<AtcTelemetryCodepathProto.CodePathClientToServer>()
            .WithDiscriminator(m => m.PayloadCase)
            .SendMessagesOfType<AtcTelemetryCodepathProto.CodePathServerToClient>()
            .ListenOn(listenPortNumber, urlPath: "/telemetry")
            .BindToServiceInstance(_service)
            .Create(telemetry, out var taskSynchronizer);

        return endpoint;
    }

    private async Task RunBuffersPumpingLoop()
    {
        try
        {
            await Task.Delay(_delayBeforeFirstPush ?? TimeSpan.FromMilliseconds(10), _cancellation.Token);
            await foreach (var buffer in _buffers.Reader.ReadAllAsync(_cancellation.Token))
            {
                Broadcast(buffer);
            }
        }
        catch (OperationCanceledException)
        {
            while (_buffers.Reader.TryRead(out var bufferToFlush))
            {
                Broadcast(bufferToFlush);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"CRITICAL ERROR in CodePathWebSocketExporter! {e}");
        }

        void Broadcast(MemoryStream buffer)
        {
            Interlocked.Increment(ref _totalBroadcastCount);
            var subscribersSnapshot = _subscribedObservers.Read();

            for (int i = 0; i < subscribersSnapshot.Count; i++)
            {
                try
                {
                    Interlocked.Increment(ref _totalSendBufferCount);
                    subscribersSnapshot[i].SendBuffer(buffer);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"CRITICAL ERROR in CodePathWebSocketExporter! {e}");
                }
            }
        }
    }
}