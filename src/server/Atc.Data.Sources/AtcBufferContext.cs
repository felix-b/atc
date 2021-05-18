using Atc.Data.Airports;
using Atc.Data.Control;
using Atc.Data.Navigation;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;

namespace Atc.Data.Sources
{
    public static class AtcBufferContext
    {
        public static BufferContextScope Create(out IBufferContext context)
        {
            var builder = BufferContextBuilder.Begin()
                .WithString()
                .WithType<WorldData>();

            // AircraftTypeData
            builder
                .WithType<AircraftTypeData>(alsoAsMapItemValue: true)
                .WithType<FlightModelData>();

            // AirlineData
            builder
                .WithType<AirlineData>(alsoAsVectorItem: true, alsoAsMapItemValue: true)
                .WithTypes<AirlineRouteData, AircraftData>(alsoAsVectorItem: true);
            
            // AirportData
            builder
                .WithType<AirportData>(alsoAsMapItemValue: true)
                .WithTypes<RunwayData, TaxiwayData, TaxiNodeData, TaxiEdgeData>(
                    alsoAsVectorItem: true, 
                    alsoAsMapItemValue: true)
                .WithType<ParkingStandData>(alsoAsMapItemValue: true);
                
            // ControlledAirspaceData & ControlFacilityData
            builder
                .WithType<ControlledAirspaceData>(alsoAsMapItemValue: true)
                .WithType<ControlFacilityData>(alsoAsMapItemValue: true)
                .WithType<ControllerPositionData>(alsoAsVectorItem: true);

            var scope = builder.End(out context);
            AllocateWorldDataRecord(context);
            return scope;
        }

        private static void AllocateWorldDataRecord(IBufferContext context)
        {
            context.AllocateRecord(new WorldData() {
                AirlineByIcao = context.AllocateStringMap<ZRef<AirlineData>>(),
                TypeByIcao = context.AllocateStringMap<ZRef<AircraftTypeData>>(),
                AirportByIcao = context.AllocateStringMap<ZRef<AirportData>>(),
            });
        }
    }
}