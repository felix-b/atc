using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;
using System.Collections.Generic;
using Atc.Data.Airports;
using Atc.Data.Control;
using Atc.Data.Navigation;
using Atc.Data.Primitives;

namespace Atc.Data.Sources
{
    public class ControlFacilityBuilder
    {
        public static ZRef<ControlFacilityData> AssembleAirportTower(
            ZRef<AirportData> airportRef,
            IEnumerable<ControllerPositionHeader> positionHeaders)
        {
            var output = BufferContext.Current;
            ref var airport = ref airportRef.Get();
            var icaoCode = airport.Header.Icao.Value;
            
            var towerRef = BufferContext.Current.AllocateRecord(new ControlFacilityData() {
                Airport = airportRef,
                Airspace = airport.Airspace!.Value,
                Type = ControlFacilityType.Tower,
                Name = output.AllocateString($"{icaoCode} Tower"), 
                CallSign = icaoCode[0] == 'K'
                    ? output.AllocateString(icaoCode.Substring(1)) // ICAO->FAA
                    : airport.Header.Icao,
                Positions = output.AllocateVector<ControllerPositionData>()
            });

            ref var tower = ref towerRef.Get();
            foreach (var header in positionHeaders)
            {
                tower.Positions.Add(AssemblePosition(header));
            }

            return towerRef;

            ControllerPositionData AssemblePosition(ControllerPositionHeader header) 
            {
                var position = new ControllerPositionData() {
                    Type = header.Type,
                    Facility = towerRef,
                    CallSign = output.AllocateString(GetPositionCallSign(header)),
                    Frequency = header.Frequency,
                    Boundary = header.Boundary,
                    HandoffControllers = output.AllocateVector<ZRefAny>()
                };
                return position;
            };
            
            string GetPositionCallSign(ControllerPositionHeader header)
            {
                var towerCallSign = towerRef.Get().CallSign.Value;
                return header.Type switch {
                    ControllerPositionType.ClearanceDelivery => $"{towerCallSign} Clearance",
                    ControllerPositionType.Ground => $"{towerCallSign} Ground",
                    ControllerPositionType.Local => $"{towerCallSign} Tower",
                    ControllerPositionType.Approach => $"{towerCallSign} Approach",
                    ControllerPositionType.Departure => $"{towerCallSign} Departure",
                    _ => towerCallSign
                };
            }
        }
        
        public class ControllerPositionHeader
        {
            public ControllerPositionHeader()
            {
                CallSign = string.Empty;
            }
    
            public ControllerPositionType Type { get; init; }
            public Frequency Frequency { get; init; }
            public string CallSign { get; init; }
            public GeoPolygon Boundary { get; init; }
        }
    }
}
