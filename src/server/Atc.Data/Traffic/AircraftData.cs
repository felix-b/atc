using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct AircraftData
    {
        // Either ModeS value (1) or Grain/Counter tuple (2).
        // (1) When ModeS is present the ID always equals ModeS
        // (2) When ModeS isn't present, the high byte (bits 25-31) specifies Grain ID (1..255)
        //     The low 3 bytes are assigned by an ever-increasing counter within the grain
        //     Since every grain has at most 1 leader at a time, no ID collisions are possible
        public uint Id { get; init; }  
        
        public ZRef<AircraftTypeData> Type { get; init; }
        public ZStringRef TailNo { get; init; }

        // contains the 24-bit Mode S value
        // or null if Mode S isn't present
        public uint? ModeS { get; init; } 
        
        public AircraftCategories Category { get; init; }
        public OperationTypes Operations { get; init; }
        public ZRef<AirlineData>? Airline { get; init; }
        public ZStringRef LiveryId { get; init; }
    }
}
