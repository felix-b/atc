using System;
using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct RunwayData
    {
        public ZStringRef Name;
        public UInt64 BitmaskFlag; // identify runway by unique position of the '1' bit, 1..64 
        public RunwayEndData End1;
        public RunwayEndData End2;
        public Distance Length;
        public Distance Width;
    }
}
