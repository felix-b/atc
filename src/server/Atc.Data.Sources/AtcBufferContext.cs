using System.IO;
using Atc.Data.Airports;
using Atc.Data.Control;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Sources
{
    public static class AtcBufferContext
    {
        public static BufferContextScope CreateEmpty(out IBufferContext context)
        {
            var builder = BufferContextBuilder.Begin()
                .WithString()
                .WithType<WorldData>();
            
            // Airports
            
            builder
                .WithType<AirportData>(alsoAsMapItemValue: true)
                .WithTypes<RunwayData, TaxiwayData, TaxiNodeData, TaxiEdgeData>(
                    alsoAsVectorItem: true, 
                    alsoAsMapItemValue: true)
                .WithType<ParkingStandData>(alsoAsMapItemValue: true);
                
            // Traffic
            
            builder
                .WithType<AircraftTypeData>(alsoAsMapItemValue: true)
                .WithType<FlightModelData>();
            builder
                .WithType<AirlineData>(alsoAsVectorItem: true, alsoAsMapItemValue: true)
                .WithTypes<FlightRouteData, AircraftData>(alsoAsVectorItem: true);
            
            // Control
            
            builder
                .WithType<ControlledAirspaceData>(alsoAsMapItemValue: true)
                .WithType<ControlFacilityData>(alsoAsMapItemValue: true)
                .WithType<ControllerPositionData>(alsoAsVectorItem: true)
                .WithType<GeoPolygon.Edge>(alsoAsVectorItem: true);

            // Navigation
   
            builder
                .WithType<IcaoRegionData>(alsoAsMapItemValue: true);
            
            var scope = builder.End(out context);
            AllocateWorldDataRecord(context);
            return scope;
        }

        public static BufferContextScope LoadFrom(Stream stream, out IBufferContext context)
        {
            context = BufferContext.ReadFrom(stream);
            return new BufferContextScope(context);
        }

        private static void AllocateWorldDataRecord(IBufferContext context)
        {
            context.AllocateRecord(new WorldData() {
                AirlineByIcao = context.AllocateStringMap<ZRef<AirlineData>>(),
                TypeByIcao = context.AllocateStringMap<ZRef<AircraftTypeData>>(),
                AirportByIcao = context.AllocateStringMap<ZRef<AirportData>>(),
                RegionByIcao =  context.AllocateStringMap<ZRef<IcaoRegionData>>(),
            });
        }
    }
}