using System;
using System.IO;
using System.Threading;
using Atc.Data.World;
using Atc.Data.World.Airports;
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

            world.Airports.Count.Should().Be(2);

            ref AirportData abcd = ref world.Airports[0];

            abcd.Icao.Value.Should().Be("ABCD");
            abcd.Datum.Lat.Should().Be(30);
            abcd.Datum.Lon.Should().Be(40);
            
            ref AirportData efgh = ref world.Airports[1];

            efgh.Icao.Value.Should().Be("EFGH");
            efgh.Datum.Lat.Should().Be(50);
            efgh.Datum.Lon.Should().Be(60);
        }
        
        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                .WithString()
                    .WithType<WorldData>()
                    .WithType<AirportData>(alsoAsVectorItem: true)
                .End(out contextTemp);

            context = (BufferContext)contextTemp;
            return scope;
        }
        
        private void WriteSampleData(string filePath)
        {
            using var scope = CreateContextScope(out var context);

            context.AllocateRecord(new WorldData {
                Airports = context.AllocateVector(new[] {
                    context.AllocateRecord(new AirportData {
                        Icao = context.AllocateString("ABCD"),
                        Datum = new() { Lat = 30, Lon = 40 }
                    }),
                    context.AllocateRecord(new AirportData {
                        Icao = context.AllocateString("EFGH"),
                        Datum = new() { Lat = 50, Lon = 60 }
                    })
                })
            });

            using (var file = File.Create(filePath))
            {
                context.WriteTo(file);
                file.Flush();
            }
        }
    }
}