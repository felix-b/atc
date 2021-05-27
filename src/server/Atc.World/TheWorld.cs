using System;
using System.Collections;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Zero.Latency.Servers;

namespace Atc.World
{
    public interface IReadOnlyWorld
    {
        IObservableQuery<Aircraft> QueryTraffic(in GeoRect rect);
    }

    public class TheWorld : IReadOnlyWorld
    {
        

        public void ProgressTo(DateTime timestamp)
        {
            // 1. create new empty ChangeSet and make it current in the context
            // 2. call ProgressTo on all parties involved   
            //    ?> abstract parties subscribing to ProgressTo?
            //    ?> pipeline?
            // 3. every party updates its state Redux-style:
            //    - an event is dispatched to a store
            //    - a reducer processes the event and produces new state
            //    - the event is published to the current ChangeSet
            //    - changes to state are published to the current ChangeSet 
            //    - existing subscriptions are run against the ChangeSet and the observers are invoked as necessary 
            // 4. 
        }

        public IObservableQuery<Aircraft> QueryTraffic(in GeoRect rect)
        {
            throw new NotImplementedException();
        }
    }
}
