using Atc.Data.Control;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct AirportData
    {
        public struct HeaderData
        {
            public ZStringRef Icao;
            public ZStringRef Name;
            public GeoPoint Datum;
            public Altitude Elevation;
        }

        public HeaderData Header;
        public ZStringMapRef<ZRef<RunwayData>> RunwayByName;
        public ZStringMapRef<ZRef<TaxiwayData>> TaxiwayByName;
        public ZIntMapRef<ZRef<TaxiNodeData>> TaxiNodeById;
        public ZStringMapRef<ZRef<ParkingStandData>> ParkingStandByName;
        public ZRef<ControlledAirspaceData>? Airspace;
        public ZRef<ControlFacilityData>? Tower;
    }
}
