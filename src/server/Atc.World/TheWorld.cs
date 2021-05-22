using System;
using System.Collections;
using System.Collections.Generic;
using Atc.Data.Primitives;

namespace Atc.World
{
    public interface IReadOnlyWorld
    {
        IObservableQuery<GeoRect, IEnumerable<Aircraft>> Traffic { get; }
    }
    

    public class TheWorld : IReadOnlyWorld
    {
        public IObservableQuery<GeoRect, IEnumerable<Aircraft>> Traffic
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
