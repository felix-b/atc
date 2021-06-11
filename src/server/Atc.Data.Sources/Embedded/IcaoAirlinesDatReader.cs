using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Sources.Embedded
{
    public class IcaoAirlinesDatReader
    {
        private readonly IEmbeddedDataSourcesLogger _logger;

        public IcaoAirlinesDatReader(IEmbeddedDataSourcesLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<ZRef<AirlineData>> ReadAirlinesDat(Stream airlinesDatFile)
        {
            var context = BufferContext.Current;
            List<ZRef<AirlineData>> results = new(capacity: 7000);

            var buffer = context.GetBuffer<AirlineData>();
            ref var worldData = ref context.GetWorldData();

            var reader = new StreamReader(airlinesDatFile, leaveOpen: true);
            var addedCount = 0;
            var skippedCount = 0;

            while (!reader.FastEndOfStream())
            {
                reader.ExtractWhitespace(includeCrlf: true);
                if (!reader.TryExtract(out string icao))
                {
                    break;
                }

                reader.Extract(out string callSign, out string region, out string name);

                var regionRef = context.AllocateString(region); 
                var newRef = buffer.Allocate(new AirlineData() {
                    Icao = context.AllocateString(icao),
                    Callsign = context.AllocateString(callSign),
                    Name = context.AllocateString(name),
                    Region = worldData.RegionByIcao.Contains(regionRef) 
                        ? worldData.RegionByIcao[regionRef] 
                        : null
                });
            
                results.Add(newRef);
                addedCount++;
            }

            _logger.DoneReadingAirlines(loaded: addedCount, skippedCount: skippedCount, wereFixed: 0);
            return results;
        }
    }
}