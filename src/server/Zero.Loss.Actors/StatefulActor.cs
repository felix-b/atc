namespace Zero.Loss.Actors
{
    public abstract class StatefulActor<TState> : IStatefulActor
        where TState : class
    {
        private readonly string _uniqueId;
        private TState _state;

        protected StatefulActor(string uniqueId, TState initialState)
        {
            _uniqueId = uniqueId;
            _state = initialState;
        }

        object IStatefulActor.GetState()
        {
            return _state;
        }

        object IStatefulActor.Reduce(object state, IStateEvent @event)
        {
            return Reduce((TState) state, @event);
        }

        void IStatefulActor.SetState(object newState)
        {
            _state = (TState)newState;
        }

        void IStatefulActor.ObserveChanges(object oldState, object newState)
        {
            ObserveChanges((TState)oldState, (TState)newState);
        }

        public string UniqueId => _uniqueId;

        protected abstract TState Reduce(TState state, IStateEvent @event);

        protected virtual void ObserveChanges(TState oldState, TState newState)
        {
            // nothing
        }
        
        protected TState State => _state;
    }
}

