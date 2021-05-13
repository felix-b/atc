using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Atc.Data.Tests
{

    public class WorldDataBufferTests
    {
        [Test]
        public void Test1()
        {
            var original = new WorldDataBuffer(new WorldData()
            {
                Airports = new List<AirportData>()
                {
                    new AirportData()
                    {
                        Icao = "ABCD",
                        Datum = new GeoPoint() {Lat = 10, Lon = 20}
                    },
                    new AirportData()
                    {
                        Icao = "EFGH",
                        Datum = new GeoPoint() {Lat = 30, Lon = 40}
                    }
                }
            });

            var stream = new MemoryStream();
            original.WriteTo(stream);
            stream.Position = 0;
            Console.WriteLine(stream.Length);

            var deserialized = WorldDataBuffer.ReadFrom(stream);
            
            Assert.That(deserialized.Data, Is.Not.Null);
            Assert.That(deserialized.Data.Airports, Is.Not.Null);
            // Assert.That(deserialized.Data.Airports.Count, Is.EqualTo(2));
            // Assert.That(deserialized.Data.Airports[0].Icao, Is.EqualTo("ABCD"));
            // Assert.That(deserialized.Data.Airports[1].Icao, Is.EqualTo("EFGH"));
        }
    }
}