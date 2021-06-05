using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public class DeferredConnectionContext<TEnvelopeIn, TEnvelopeOut> : IDeferredConnectionContext<TEnvelopeOut> 
        where TEnvelopeIn : class 
        where TEnvelopeOut : class 
    {
        private readonly IConnectionContext _inner;
        private List<OutputRequest>? _outputRequests = null;

        public DeferredConnectionContext(IConnectionContext inner)
        {
            _inner = inner;
        }

        void IDeferredConnectionContext<TEnvelopeOut>.FireMessage(TEnvelopeOut outgoingMessageEnvelope)
        {
            AddOutputRequest(new FireMessageRequest(outgoingMessageEnvelope)); 
        }

        void IDeferredConnectionContext<TEnvelopeOut>.RegisterObserver(IObserverSubscription observer, string? registrationKey)
        {
            AddOutputRequest(new RegisterObserverRequest(observer, registrationKey)); 
        }

        void IDeferredConnectionContext<TEnvelopeOut>.DisposeObserver(string registrationKey)
        {
            AddOutputRequest(new DisposeObserverRequest(registrationKey)); 
        }

        void IDeferredConnectionContext<TEnvelopeOut>.RequestClose()
        {
            AddOutputRequest(new CloseConnectionRequest()); 
        }

        void IDeferredConnectionContext<TEnvelopeOut>.RequestFlush()
        {
            OnFlushRequested?.Invoke();
        }

        public async ValueTask PerformOutputRequests()
        {
            if (_outputRequests is null)
            {
                return;
            }
            
            foreach (var request in _outputRequests)
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

            _outputRequests = null;
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

        public Action OnFlushRequested { get; set; } = () => { };
        
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
    }
}
