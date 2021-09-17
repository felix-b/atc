using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;

namespace Atc.Data.Sources.Embedded
{
    public class TypeJsonReader
    {
        private readonly IEmbeddedDataSourcesLogger _logger;

        public TypeJsonReader(IEmbeddedDataSourcesLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<ZRef<AircraftTypeData>> ReadTypeJson(Stream input)
        {
            return Array.Empty<ZRef<AircraftTypeData>>();
            
            // using var reader = new StreamReader(input, leaveOpen: true);
            // var json = reader.ReadToEnd();
            // var deserialized = JsonSerializer.Deserialize<IList<TypeObject>>(json);
            //
            // throw new NotImplementedException();
        }

        public class TypeObject
        {
            
        }
    }
}