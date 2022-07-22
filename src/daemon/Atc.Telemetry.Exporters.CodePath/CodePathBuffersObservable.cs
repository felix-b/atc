using Atc.Server;

namespace Atc.Telemetry.Exporters.CodePath;

public class CodePathBuffersObservable : 
    IObservableQuery<MemoryStream>,
    IObserverSubscription, 
    ICodePathBuffersObserver
{
    private readonly CodePathWebSocketExporter _ownerExporter;
    private QueryObserver<MemoryStream>? _callback;

    public CodePathBuffersObservable(CodePathWebSocketExporter ownerExporter)
    {
        _ownerExporter = ownerExporter;
    }

    public ValueTask DisposeAsync()
    {
        _ownerExporter.UnsubscribeObserver(this);
        return ValueTask.CompletedTask;
    }

    public IObserverSubscription Subscribe(QueryObserver<MemoryStream> callback)
    {
        _callback = callback;
        _ownerExporter.SubscribeObserver(this);
        return this;
    }

    public IEnumerable<MemoryStream> GetResults()
    {
        return Array.Empty<MemoryStream>();
    }

    public void SendBuffer(MemoryStream buffer)
    {
        if (_callback == null)
        {
            return;
        }

        var observation = new QueryObservation<MemoryStream>(
            added: new[] {buffer},
            updated: Array.Empty<MemoryStream>(),
            removed: Array.Empty<MemoryStream>());
        
        _callback.Invoke(observation);
    }
}
