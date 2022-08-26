using Atc.Telemetry;

namespace Atc.Server;

public class DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> : IDeferredConnectionContext<TEnvelopeOut> 
    where TEnvelopeIn : class 
    where TEnvelopeOut : class 
{
    private readonly IConnectionContext _inner;
    private readonly IEndpointTelemetry _telemetry;
    private readonly FlushRequestedCallback? _onFlushRequested;
    private List<OutputRequest>? _outputRequests = null;

    public DeferredConnectionContext(
        IConnectionContext inner, 
        IEndpointTelemetry telemetry, 
        FlushRequestedCallback? onFlushRequested)
    {
        _inner = inner;
        _telemetry = telemetry;
        _onFlushRequested = onFlushRequested;
    }

    IDeferredConnectionContext<TEnvelopeOut> IDeferredConnectionContext<TEnvelopeOut>.CopyForPush()
    {
        return new DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut>(_inner, _telemetry, _onFlushRequested);
    }

    void IDeferredConnectionContext<TEnvelopeOut>.FireMessage(TEnvelopeOut outgoingMessageEnvelope)
    {
        _telemetry.DebugConnectionFireMessageRequest(connectionId: _inner.Id);
        AddOutputRequest(new FireMessageRequest(outgoingMessageEnvelope)); 
    }

    void IDeferredConnectionContext<TEnvelopeOut>.RegisterObserver(IObserverSubscription observer, string? registrationKey)
    {
        _telemetry.DebugConnectionRegisterObserverRequest(connectionId: _inner.Id, registrationKey: registrationKey ?? string.Empty);
        AddOutputRequest(new RegisterObserverRequest(observer, registrationKey)); 
    }

    void IDeferredConnectionContext<TEnvelopeOut>.DisposeObserver(string registrationKey)
    {
        if (registrationKey == null)
        {
            throw _telemetry.ExceptionDisposeObserverRegistrationKeyNull();
        }
        
        _telemetry.DebugConnectionDisposeObserverRequest(connectionId: _inner.Id, registrationKey: registrationKey);
        AddOutputRequest(new DisposeObserverRequest(registrationKey)); 
    }

    void IDeferredConnectionContext<TEnvelopeOut>.RequestClose()
    {
        _telemetry.DebugConnectionCloseRequest(connectionId: _inner.Id);
        AddOutputRequest(new CloseConnectionRequest()); 
    }

    void IDeferredConnectionContext<TEnvelopeOut>.RequestFlush()
    {
        _telemetry.DebugConnectionFlushRequest(connectionId: _inner.Id);
        _onFlushRequested?.Invoke(this);
    }

    public async ValueTask PerformOutputRequests()
    {
        if (_outputRequests is null)
        {
            return;
        }

        int requestIndex = 0;
        
        foreach (var request in _outputRequests)
        {
            using var traceSpan = _telemetry.SpanConnectionPerformOutputRequest(
                index: requestIndex++, 
                type: request.GetType().Name);

            try
            {
                await PerformOneOutputRequest(request);
            }
            catch (Exception e)
            {
                traceSpan.Fail(e);
                throw;
            }
        }

        ClearOutputRequests();

        async Task PerformOneOutputRequest(OutputRequest request)
        {
            switch (request)
            {
                case CloseConnectionRequest:
                    await _inner.CloseConnection();
                    break;
                case FireMessageRequest messageRequest:
                    await _inner.SendMessage(messageRequest.Envelope);
                    break;
                case RegisterObserverRequest observerRequest:
                    _inner.RegisterObserver(observerRequest.Observer, observerRequest.RegistrationKey);
                    break;
                case DisposeObserverRequest disposeRequest:
                    await _inner.DisposeObserver(disposeRequest.RegistrationKey);
                    break;
            }
        }
    }

    public void ClearOutputRequests()
    {
        _outputRequests = null;
    }

    public long Id => _inner.Id;

    public bool IsActive => _inner.IsActive;

    public CancellationToken Cancellation => _inner.Cancellation;

    public SessionItems Session => _inner.Session;

    public IReadOnlyList<OutputRequest>? OutputRequests => _outputRequests;

    public abstract record OutputRequest; 
    public record CloseConnectionRequest : OutputRequest; 
    public record FireMessageRequest(TEnvelopeOut Envelope) : OutputRequest;
    public record RegisterObserverRequest(IObserverSubscription Observer, string? RegistrationKey) : OutputRequest;
    public record DisposeObserverRequest(string RegistrationKey) : OutputRequest;

    private void AddOutputRequest(OutputRequest request)
    {
        if (_outputRequests is null)
        {
            _outputRequests = new(capacity: 4);
        }
            
        _outputRequests.Add(request);
    }

    public delegate void FlushRequestedCallback(DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> copyOfContext);
}
