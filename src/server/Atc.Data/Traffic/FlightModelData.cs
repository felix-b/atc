using Atc.Data.Primitives;

namespace Atc.Data.Traffic
{
    public struct FlightModelData
    {
        public Speed V1 { get; init; }
        public Speed V2 { get; init; }
        public Speed Vx { get; init; }
        public Speed Vy { get; init; }
        public Speed Vref { get; init; }
        public Speed Vfe { get; init; }
        public Speed Vne { get; init; }
        public Speed Vno { get; init; }
    }
}
