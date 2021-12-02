using System;
using System.Collections.Immutable;
using System.Net;
using Just.Utility;
using Zero.Doubt.Logging;

namespace Zero.Loss.Actors.Impl
{
    public class StateStore : IStateStore, IStateStoreInit, IInternalStateStore
    {
        private readonly ILogger _logger;
        private readonly WriteLocked<EventListenerCollection> _eventListeners;
        
        private ulong _nextSequenceNo = 1;
        private bool _dispatching = false;
        

        public StateStore(ILogger logger)
        {
            _logger = logger;
            _eventListeners = new WriteLocked<EventListenerCollection>(new EventListenerCollection(
                NextListenerId: 1, 
                Listeners: ImmutableArray<EventListenerItem>.Empty));
        }

        public void Dispatch(IStatefulActor target, IStateEvent @event)
        {
            if (_dispatching)
            {
                throw new InvalidOperationException("Reentrant dispatch detected. Use World.Defer() instead.");
            }

            var sequenceNo = _nextSequenceNo++;
            using var logSpan = _logger.Dispatch(sequenceNo, target.UniqueId, @event.GetType().Name, @event.ToString());
            _dispatching = true;

            try
            {
                var state0 = target.GetState();
                var state1 = target.Reduce(state0, @event);

                if (!object.ReferenceEquals(state0, state1))
                {
                    InvokeEventListeners(new StateEventEnvelope(sequenceNo, target.UniqueId, @event));
                    
                    target.SetState(state1);
                    target.ObserveChanges(state0, state1);
                }
            }
            catch (Exception e)
            {
                logSpan.Fail(e);
            }
            finally
            {
                _dispatching = false;
            }
        }

        public void AddEventListener(StateEventListener listener, out int listenerId)
        {
            var collectionBefore = _eventListeners.Exchange(collection => collection.WithListener(listener));
            listenerId = collectionBefore.NextListenerId;
        }

        public void RemoveEventListener(int listenerId)
        {
            _eventListeners.Replace(collection => collection.WithoutListener(listenerId));
        }

        public void ResetNextSequenceNo(ulong value)
        {
            _logger.ResetNextSequenceNo(value);
            _nextSequenceNo = value;
        }

        public ulong NextSequenceNo => _nextSequenceNo;
        private void InvokeEventListeners(in StateEventEnvelope envelope)
        {
            var listenersSnapshot = _eventListeners.Read().Listeners;
            var count = listenersSnapshot.Length;
            
            for (int i = 0; i < count; i++)
            {
                try
                {
                    listenersSnapshot[i].Callback(envelope);
                }
                catch (Exception e)
                {
                    _logger.ListenerFailed(envelope.SequenceNo, listenersSnapshot[i].Id, envelope.Event.GetType().Name, e);
                }
            }
        }

        private record EventListenerCollection(int NextListenerId, ImmutableArray<EventListenerItem> Listeners)
        {
            public EventListenerCollection WithListener(StateEventListener listener)
            {
                var listenerId = this.NextListenerId;
                var item = new EventListenerItem(listenerId, listener);
                return new EventListenerCollection(NextListenerId: listenerId + 1, Listeners: this.Listeners.Add(item));
            }

            public EventListenerCollection WithoutListener(int listenerId)
            {
                return new EventListenerCollection(
                    this.NextListenerId, 
                    this.Listeners.RemoveAll(item => item.Id == listenerId));
            }
        }
        
        private record EventListenerItem(int Id, StateEventListener Callback);

        public interface ILogger
        {
            LogWriter.LogSpan Dispatch(ulong sequenceNo, string targetId, string eventType, string? eventData);
            void ListenerFailed(ulong sequenceNo, int listenerId, string eventType, Exception error);
            void ResetNextSequenceNo(ulong value);
        }
    }
}
