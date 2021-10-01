#if false
using System.Collections.Generic;

namespace Zero.Loss.Actors.Tests
{
    public class SimpleEventBuffer
    {
        private readonly List<IStateEvent> _events = new();
        
        public void Add(IStateEvent @event)
        {
            _events.Add(@event);
        }

        public IReadOnlyList<IStateEvent> Events => _events;
    }
}
#endif