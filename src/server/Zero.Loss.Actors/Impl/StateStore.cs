using System;
using System.Collections.Immutable;
using Zero.Doubt.Logging;

namespace Zero.Loss.Actors.Impl
{
    public class StateStore : IStateStore, IStateStoreInit
    {
        private readonly ILogger _logger;
        private ImmutableArray<EventListenerEntry> _eventListeners = ImmutableArray<EventListenerEntry>.Empty;
        private ulong _nextSequenceNo = 1;
        private int _nextEventListenerId = 1;

        public StateStore(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispatch(IStatefulActor target, IStateEvent @event)
        {
            var sequenceNo = _nextSequenceNo++;
            using var logSpan = _logger.Dispatch(sequenceNo, target.UniqueId, @event.GetType().Name, @event.ToString());

            try
            {
                var state0 = target.GetState();
                var state1 = target.Reduce(state0, @event);

                if (!object.ReferenceEquals(state0, state1))
                {
                    InvokeEventListeners(sequenceNo, @event);
                    target.SetState(state1);
                    target.ObserveChanges(state0, state1);
                }
            }
            catch (Exception e)
            {
                logSpan.Fail(e);
            }
        }

        public void AddEventListener(StateEventListener listener, out int listenerId)
        {
            listenerId = _nextEventListenerId++;
            _eventListeners = _eventListeners.Add(new EventListenerEntry(listenerId, listener));
        }

        public void RemoveEventListener(int listenerId)
        {
            _eventListeners = _eventListeners.RemoveAll(entry => entry.Id == listenerId);
        }

        private void InvokeEventListeners(ulong sequenceNo, IStateEvent @event)
        {
            var listenersSnapshot = _eventListeners;
            var count = listenersSnapshot.Length;
            
            for (int i = 0; i < count; i++)
            {
                try
                {
                    listenersSnapshot[i].Callback(sequenceNo, @event);
                }
                catch (Exception e)
                {
                    //TODO: log error
                }
            }
        }

        private record EventListenerEntry(int Id, StateEventListener Callback);

        public interface ILogger
        {
            LogWriter.LogSpan Dispatch(ulong sequenceNo, string targetId, string eventType, string eventData);
            void ListenerFailed(ulong sequenceNo, string eventType, Exception error);
        }
    }
}
