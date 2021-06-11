using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atc.Data.Navigation;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Sources.Embedded
{
    public class IcaoRegionCodesDatReader
    {
        private readonly IEmbeddedDataSourcesLogger _logger;

        public IcaoRegionCodesDatReader(IEmbeddedDataSourcesLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<ZRef<IcaoRegionData>> ReadRegions(Stream regionCodesDatFile)
        {
            var context = BufferContext.Current;
            var buffer = context.GetBuffer<IcaoRegionData>();
            var reader = new StreamReader(regionCodesDatFile, leaveOpen: true);
            var countriesByCode = new Dictionary<string, List<string>>();
            string? line = null;

            do
            {
                line = reader.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                {
                    AddEntry(code: parts[0], country: parts[1]);
                }
                else
                {
                    _logger.IcaoRegionCodeError(line);
                }
            } while (line != null);

            return PopulateRegionDataRecords();

            void AddEntry(string code, string country)
            {
                if (countriesByCode.TryGetValue(code, out var existingCountryList))
                {
                    existingCountryList.Add(country);
                }
                else
                {
                    countriesByCode[code] = new() { country };
                }
            }

            List<ZRef<IcaoRegionData>> PopulateRegionDataRecords()
            {
                var results = new List<ZRef<IcaoRegionData>>();
                
                foreach (var pair in countriesByCode!)
                {
                    var countries = pair.Value;
                    var data = new IcaoRegionData() {
                        Code = context!.AllocateString(pair.Key),
                        Country = context!.AllocateString(countries[0]),
                        Country2 = countries.Count >= 2 ? context!.AllocateString(countries[1]) : null,
                        Country3 = countries.Count >= 3 ? context!.AllocateString(countries[2]) : null
                    };
                    results.Add(buffer!.Allocate(data));
                }

                return results;
            }
        }
    }
}