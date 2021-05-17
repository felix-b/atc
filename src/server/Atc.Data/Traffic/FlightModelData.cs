using Atc.Data.Primitives;

namespace Atc.Data.Traffic
{
    public readonly struct FlightModelData
    {
        public readonly Speed V1;
        public readonly Speed V2;
        public readonly Speed Vx;
        public readonly Speed Vy;
        public readonly Speed Vref;
        public readonly Speed Vfe;
        public readonly Speed Vne;
        public readonly Speed Vno;
    }
}
