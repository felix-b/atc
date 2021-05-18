using System;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Sources
{
    public class AirspaceBuilder
    {
        public static ZRef<ControlledAirspaceData> AssembleSimpleAirspace(
            AirspaceType type,
            AirspaceClass classification,
            ZStringRef name,
            ZStringRef areaCode,
            ZStringRef icaoCode,
            ZStringRef? centerName,
            GeoPoint centerPoint,
            Distance radius,
            Altitude? lowerLimit,
            Altitude? upperLimit)
        {
            var geometry = new AirspaceGeometry() {
                LateralBounds = GeoPolygon.FromEdges(
                    GeoPolygon.Edge.FromCircle(centerPoint, radius)
                ), 
                LowerBound = lowerLimit,
                UpperBound = upperLimit,
            };
            var airspaceRef = BufferContext.Current.AllocateRecord(new ControlledAirspaceData() {
                Id = 1,
                AreaCode = areaCode,
                IcaoCode = icaoCode,
                CenterName = centerName,
                Name = name, 
                Type = type, 
                Class = classification,
                Geometry = geometry
            });
            
            return airspaceRef;
        }
    }
}