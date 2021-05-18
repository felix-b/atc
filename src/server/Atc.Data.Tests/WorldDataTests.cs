using System;
using System.IO;
using System.Threading;
using Atc.Data.Airports;
using Atc.Data.Traffic;
using FluentAssertions;
using NUnit.Framework;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Tests
{
    [TestFixture]
    public class WorldDataTests
    {
        [Test]
        public void SampleDataDiskRoundtrip()
        {
            var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "atc-world.cache");
            Console.WriteLine(filePath);

            WriteSampleData(filePath);
            Thread.Sleep(500);
            using var file = File.OpenRead(filePath);
            
            var context = BufferContext.ReadFrom(file);
            using var scope = new BufferContextScope(context);

            scope.GetBuffer<WorldData>().RecordCount.Should().Be(1);
            
            ref WorldData world = ref scope.GetBuffer<WorldData>()[0];

            world.AirportByIcao.Count.Should().Be(2);

            // ref AirportData abcd = ref world.AirportByIcao[0];
            //
            // abcd.Icao.Value.Should().Be("ABCD");
            // abcd.Datum.Lat.Should().Be(30);
            // abcd.Datum.Lon.Should().Be(40);
            //
            // ref AirportData efgh = ref world.Airports[1];
            //
            // efgh.Icao.Value.Should().Be("EFGH");
            // efgh.Datum.Lat.Should().Be(50);
            // efgh.Datum.Lon.Should().Be(60);
        }
        
        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                .WithString()
                    .WithString()
                    .WithType<WorldData>()
                    .WithTypes<AirportData, AirlineData, AircraftTypeData>(alsoAsVectorItem: true)
                    .WithMapTo<ZRef<AirlineData>>()
                    .WithMapTo<ZRef<AirportData>>()
                    .WithMapTo<ZRef<AircraftTypeData>>()
                .End(out contextTemp);

            context = (BufferContext)contextTemp;
            return scope;
        }
        
        private void WriteSampleData(string filePath)
        {
            using var scope = CreateContextScope(out var context);

            var dataRef = context.AllocateRecord(new WorldData {
                AirportByIcao = context.AllocateStringMap<ZRef<AirportData>>(),
                AirlineByIcao = context.AllocateStringMap<ZRef<AirlineData>>(),
                TypeByIcao = context.AllocateStringMap<ZRef<AircraftTypeData>>(),
            });

            var abcdString = context.AllocateString("ABCD");
            dataRef.Get().AirportByIcao.Add(
                abcdString,
                context.AllocateRecord(new AirportData {
                    Header = new AirportData.HeaderData {
                        Icao = abcdString, 
                        Datum = new(30, 40)
                    }
                })
            );
            

            var efghString = context.AllocateString("EFGH");
            dataRef.Get().AirportByIcao.Add(
                efghString,
                context.AllocateRecord(new AirportData {
                    Header = new AirportData.HeaderData {
                        Icao = efghString, 
                        Datum = new(50, 60)
                    }
                })
            );

            using (var file = File.Create(filePath))
            {
                context.WriteTo(file);
                file.Flush();
            }
        }
    }
}