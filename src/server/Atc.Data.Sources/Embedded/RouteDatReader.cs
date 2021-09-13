using System.Collections.Generic;
using System.IO;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;

namespace Atc.Data.Sources.Embedded
{
    public class RouteDatReader
    {
        public IEnumerable<ZRef<FlightRouteData>> ReadRouteDat(Stream input)
        {
            throw new System.NotImplementedException();
        }
    }
}
