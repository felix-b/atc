using Zero.Serialization.Buffers;
using Atc.Data.Airports;
using Atc.Data.Navigation;

namespace Atc.Data.Control
{
    public struct ControlFacilityData
    {
        public ZStringRef CallSign;
        public ZStringRef Name;
        public ControlFacilityType Type;
        public ZRef<ControlledAirspaceData> Airspace;
        public ZRefAny? Airport;
        public ZVectorRef<ControllerPositionData> Positions;

        //TODO:
        //vector<shared_ptr<InformationService>> m_services;
    }
}
