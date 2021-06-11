using System.Collections.Generic;
using Zero.Serialization.Buffers;

namespace Atc.Data.Navigation
{
    public struct IcaoRegionData
    {
        public ZStringRef Code;
        public ZStringRef Country;
        public ZStringRef? Country2;
        public ZStringRef? Country3;

        public IEnumerable<ZStringRef> Countries
        {
            get
            {
                yield return Country;

                if (Country2.HasValue)
                {
                    yield return Country2.Value;
                }
                
                if (Country3.HasValue)
                {
                    yield return Country3.Value;
                }
            }
        }
    }
}
