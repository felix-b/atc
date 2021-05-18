using System.IO;
using Atc.Data.Airports;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Atc.Data.Sources.XP.Airports;
using NUnit.Framework;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Sources.Tests.XP.Airports
{
    [TestFixture]
    public class AptDataReaderTests
    {
        [Test]
        public void ReadRealAirport_HUEN()
        {
            using var scope = AtcBufferContext.Create(out var context);
            using var aptDat = File.Open(
                TestContext.CurrentContext.GetTestInputPath("apt_huen.dat"),
                FileMode.Open);

            var aptDatReader = new AptDatReader(context, OnQueryAirspace);
            aptDatReader.ReadAirport(aptDat);

            var airportRef = aptDatReader.GetAirport();
            using var outputFile = File.Open(
                TestContext.CurrentContext.GetTestOutputPath("huen.atc.cache"),
                FileMode.Create);

            ((BufferContext)context).WriteTo(outputFile);
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