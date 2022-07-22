using System.Text;
using Atc.Server;
using Atc.Telemetry.CodePath;
using AtcTelemetryCodepathProto;

namespace Atc.Telemetry.Exporters.CodePath;

public class CodePathExporterService
{
    private readonly CodePathWebSocketExporter _exporter;
    private ulong _totalFireMessageCount = 0;
    private ulong _totalObserveBuffersCount = 0;
    private ulong _totalTelemetryBytes = 0;

    public CodePathExporterService(CodePathWebSocketExporter exporter)
    {
        _exporter = exporter;
    }

    [PayloadCase(CodePathClientToServer.PayloadOneofCase.connect_request)]
    public void Connect(
        IDeferredConnectionContext<CodePathServerToClient> connection, 
        CodePathClientToServer envelope)
    {
        var replyEnvelope = new CodePathServerToClient() {
            connect_reply = new CodePathServerToClient.ConnectReply() {
                Success = true,
                Error = string.Empty
            }
        };

        connection.FireMessage(replyEnvelope);

        bool isFirstObserverInvocation = true;
        var observable = new CodePathBuffersObservable(_exporter);
        var subscription = observable.Subscribe(ObserveBuffers);
        connection.RegisterObserver(subscription, connection.Id.ToString());

        void ObserveBuffers(in QueryObservation<MemoryStream> observation)
        {
            Interlocked.Increment(ref _totalObserveBuffersCount);

            var pushConnection = connection.CopyForPush();
            if (!pushConnection.IsActive)
            {
                return;
            }

            if (isFirstObserverInvocation && _exporter.Environment != null)
            {
                isFirstObserverInvocation = false;
                SendStringMap();
            }
            
            SendObservedBuffers(observation.Added);
            pushConnection.RequestFlush();

            void SendStringMap()
            {
                var snapshot = _exporter.StringMap!.TakeSnapshot();
                var buffer = new MemoryStream();
                var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true);
                _exporter.StringMap!.WriteAllEntries(writer);
                writer.Flush();
                buffer.Flush();

                SendBuffer(buffer);
            }
            
            void SendObservedBuffers(IEnumerable<MemoryStream> observedBuffers)
            {
                foreach (var newBuffer in observedBuffers)
                {
                    SendBuffer(newBuffer);
                }
            }

            void SendBuffer(MemoryStream buffer)
            {
                var pushEnvelope = new CodePathServerToClient()
                {
                    telemetry_buffer = new CodePathServerToClient.TelemetryBuffer() {
                        Buffer = buffer.ToArray()
                    }
                };

                Interlocked.Increment(ref _totalFireMessageCount);
                Interlocked.Add(ref _totalTelemetryBytes, (ulong)buffer.Length);

                pushConnection.FireMessage(pushEnvelope);
            }
        }
    }
    
    [PayloadCase(CodePathClientToServer.PayloadOneofCase.disconnect_request)]
    public void Disconnect(
        IDeferredConnectionContext<CodePathServerToClient> connection, 
        CodePathClientToServer envelope)
    {
        connection.DisposeObserver(connection.Id.ToString());
        connection.RequestClose();
    }

    public ulong TotalFireMessageCount => _totalFireMessageCount;
    public ulong TotalObserveBuffersCount => _totalObserveBuffersCount;
    public ulong TotalTelemetryBytes => _totalTelemetryBytes;
}
