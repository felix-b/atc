#if false
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Atc.Data.Airports;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Atc.Data.Sources;
using Atc.Data.Sources.XP.Airports;
using FluentAssertions;
using Zero.Serialization.Buffers;

namespace Atc.Data.Compiler
{
    public class AtccAptDatReadTest
    {
        public const string AptDatFilePath = @"D:\TnC\atc2\src\server\Atc.Data.Sources.Tests\TestInputs\apt_huen.dat";
        
        public void ReadRealAirport_HUEN()
        {
            void FirstTime()
            {
                using var scope = AtcBufferContext.CreateEmpty(out var context);
                var airportRef = ReadSingleTestAirport(context, AptDatFilePath);
                AssertAirport_HUEN(context, airportRef);
            }

            void SecondTime()
            {
                using var scope = AtcBufferContext.CreateEmpty(out var context);
                var airportRef = ReadSingleTestAirport(context, AptDatFilePath);
                AssertAirport_HUEN(context, airportRef);
            }

            void ThirdTime()
            {
                using var scope = AtcBufferContext.CreateEmpty(out var context);
                var airportRef = ReadSingleTestAirport(context, AptDatFilePath);
                AssertAirport_HUEN(context, airportRef);
            }

            FirstTime();
            SecondTime();
            ThirdTime();
        }

        private ZRef<AirportData> ReadSingleTestAirport(IBufferContext context, string aptDatFilePath)
        {
            using var aptDat = File.Open(aptDatFilePath, FileMode.Open);

            var aptDatReader = new AirportReader(context, OnQueryAirspace);
            
            var clock = Stopwatch.StartNew();

            aptDatReader.ReadAirport(aptDat);
            var airportRef = aptDatReader.GetAirport();

            clock.Stop();
            Console.WriteLine(clock.Elapsed);

            return airportRef!.Value;
        }

        private void AssertAirport_HUEN(IBufferContext context, ZRef<AirportData> airportRef)
        {
            ref var airport = ref airportRef.Get();
            
            AssertAirportHeader(
                airportRef, 
                expectedName: "Entebbe Intl", 
                expectedIcao: "HUEN", 
                expectedDatumLat:0.040813889,
                expectedDatumLon:32.440541667,
                expectedElevationFeet:3782);

            airport.Runways.Count.Should().Be(1);
            AssertAirportRunway(
                airportRef, 
                runwayIndex:0, 
                expectedNames: new[] { "17-35", "35-17", "17", "35" }, 
                expectedWidthMeters: 55,
                expectedLengthMeters: 3656,
                expectedBitmaskFlag: 0x01,
                expectedEnd1Name: "17",
                expectedEnd1Lat: 00.05560291,
                expectedEnd1Lon: 032.43624156,
                expectedEnd1Heading: 172,
                expectedEnd1DisplacedThresholdMeters: 0,
                expectedEnd1OverrunAreaMeters: 0,
                expectedEnd2Name: "35",
                expectedEnd2Lat: 00.02285606,     
                expectedEnd2Lon: 032.44078821,
                expectedEnd2Heading: 352,
                expectedEnd2DisplacedThresholdMeters: 0,
                expectedEnd2OverrunAreaMeters: 0);

            airport.TaxiNodeById.Count.Should().Be(110);
            AssertTaxiEdge(
                ref airport, 
                fromNodeId: 3, 
                toNodeId: 92, 
                expectedName: "A1", 
                expectedType: TaxiEdgeType.Taxiway, 
                expectedWidthCode: 'D',
                expectedLengthMeters: 54,
                expectedHeadingDegrees: 339,
                expectReverseEdge: true);
            
            AssertParkingStand(
                ref airport, 
                name: "Gate 7", 
                expectedLat: 0.04597976,
                expectedLon: 032.44193697,
                expectedHeadingDegrees: 82,
                expectedType: ParkingStandType.Gate,
                expectedWidthCode: 'D',
                expectedCategories: AircraftCategories.Heavy | AircraftCategories.Jet | AircraftCategories.Turboprop | AircraftCategories.Prop,
                expectedOperations: OperationTypes.GA,
                expectedAirlinesIcao: new string[0] /*{ atc klm msr uae qtr }*/);
        }

        private void AssertAirportHeader(
            ZRef<AirportData> airportRef,
            string expectedName,
            string expectedIcao,
            double expectedDatumLat,
            double expectedDatumLon,
            int expectedElevationFeet)
        {
            ref var airport = ref airportRef.Get();
            airport.Header.Name.Value.Should().Be(expectedName);
            airport.Header.Icao.Value.Should().Be(expectedIcao);
            airport.Header.Datum.Lat.Should().Be(expectedDatumLat);
            airport.Header.Datum.Lon.Should().Be(expectedDatumLon);
            System.Math.Round(airport.Header.Elevation.Feet).Should().Be(expectedElevationFeet);
        }

        private void AssertAirportRunway(
            ZRef<AirportData> airportRef,
            int runwayIndex,
            string[] expectedNames,
            int expectedWidthMeters,
            int expectedLengthMeters,
            ulong expectedBitmaskFlag,
            string expectedEnd1Name,
            double expectedEnd1Lat,
            double expectedEnd1Lon,
            int expectedEnd1Heading,
            int expectedEnd1DisplacedThresholdMeters,
            int expectedEnd1OverrunAreaMeters,
            string expectedEnd2Name,
            double expectedEnd2Lat,
            double expectedEnd2Lon,
            int expectedEnd2Heading,
            int expectedEnd2DisplacedThresholdMeters,
            int expectedEnd2OverrunAreaMeters)
        {
            ref var airport = ref airportRef.Get();
            var runwayRef = airport.Runways[runwayIndex];
            ref var runway = ref runwayRef.Get();

            foreach (var expectedName in expectedNames)
            {
                airport.RunwayByName[expectedName].Should().Be(runwayRef);
            }

            expectedNames.Contains(runway.Name.Value).Should().BeTrue();
            System.Math.Round(runway.Width.Meters).Should().Be(expectedWidthMeters);
            System.Math.Round(runway.Length.Meters).Should().Be(expectedLengthMeters);
            runway.BitmaskFlag.Should().Be(expectedBitmaskFlag);
            
            AssertRunwayEnd(
                ref runway.End1,
                expectedEnd1Name,
                expectedEnd1Lat,
                expectedEnd1Lon,
                expectedEnd1Heading,
                expectedEnd1DisplacedThresholdMeters,
                expectedEnd1OverrunAreaMeters);
            
            AssertRunwayEnd(
                ref runway.End2,
                expectedEnd2Name,
                expectedEnd2Lat,
                expectedEnd2Lon,
                expectedEnd2Heading,
                expectedEnd2DisplacedThresholdMeters,
                expectedEnd2OverrunAreaMeters);

            static void AssertRunwayEnd(
                ref RunwayEndData end,
                string expectedName,
                double expectedLat,
                double expectedLon,
                int expectedHeading,
                int expectedDisplacedThresholdMeters,
                int expectedOverrunAreaMeters)
            {
                end.Name.Value.Should().Be(expectedName);
                end.CenterlinePoint.Lat.Should().Be(expectedLat);
                end.CenterlinePoint.Lon.Should().Be(expectedLon);
                System.Math.Round(end.Heading.Degrees).Should().Be(expectedHeading);
                end.DisplacedThresholdLength.Meters.Should().Be(expectedDisplacedThresholdMeters);
                end.OverrunAreaLength.Meters.Should().Be(expectedOverrunAreaMeters);
            }
        }

        private delegate void AssertTaxiNodeCallback(ref TaxiNodeData node);
        
        private void AssertTaxiEdge(
            ref AirportData airport,
            int fromNodeId,
            int toNodeId,
            string expectedName,
            TaxiEdgeType expectedType,
            char expectedWidthCode,
            int expectedHeadingDegrees,
            int expectedLengthMeters,
            bool expectReverseEdge,
            AssertTaxiNodeCallback? assertFromNode = null,
            AssertTaxiNodeCallback? assertToNode = null)
        {
            ref var fromNode = ref airport.TaxiNodeById[fromNodeId].Get();
            var edgeCur = fromNode.EdgesOut.SingleOrDefault(e => e.Get().Get().Node2Ref().Get().Id == toNodeId);
            edgeCur.IsNull.Should().BeFalse();
            ref var edge = ref edgeCur.Get().Get();

            edge.Name.Value.Should().Be(expectedName);
            edge.Type.Should().Be(expectedType);
            edge.WidthCode.Should().Be(expectedWidthCode);
            System.Math.Round(edge.Heading.Degrees).Should().Be(expectedHeadingDegrees);
            System.Math.Round(edge.Length.Meters).Should().Be(expectedLengthMeters);
            edge.IsOneWay.Should().Be(!expectReverseEdge);
            edge.ReverseEdge.HasValue.Should().Be(expectReverseEdge);

            ref var toNode = ref edge.Node2Ref().Get();
            
            assertFromNode?.Invoke(ref fromNode);
            assertToNode?.Invoke(ref toNode);
        }

        private void AssertParkingStand(
            ref AirportData airport,
            string name,
            double expectedLat,
            double expectedLon,
            int expectedHeadingDegrees,
            ParkingStandType expectedType,
            char expectedWidthCode,
            AircraftCategories expectedCategories,
            OperationTypes expectedOperations,
            string[] expectedAirlinesIcao)
        {
            ref var gate = ref airport.ParkingStandByName[name].Get();

            gate.Name.Value.Should().Be(name);
            gate.Location.Lat.Should().Be(expectedLat);
            gate.Location.Lon.Should().Be(expectedLon);
            System.Math.Round(gate.Direction.Degrees).Should().Be(expectedHeadingDegrees);
            gate.Type.Should().Be(expectedType);
            gate.WidthCode.Should().Be(expectedWidthCode);
            gate.Categories.Should().Be(expectedCategories);
            gate.Operations.Should().Be(expectedOperations);

            var actualAirlinesIcao = gate.Airlines.Select(al => al.Get().Get().Icao.Value).ToArray();
            actualAirlinesIcao.Should().BeEquivalentTo(expectedAirlinesIcao);
        }
        
        private ZRef<ControlledAirspaceData> OnQueryAirspace(in AirportData.HeaderData header)
        {
            return AirspaceBuilder.AssembleSimpleAirspace(
                AirspaceType.ControlZone,
                AirspaceClass.B,
                name: header.Name,
                icaoCode: header.Icao,
                centerName: header.Icao,
                areaCode: header.Icao,
                centerPoint: header.Datum,
                radius: Distance.FromNauticalMiles(10),
                lowerLimit: null,
                upperLimit: Altitude.FromFeetMsl(18000)
            );
        }
    }
}
#endif
