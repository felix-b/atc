using System;
using System.Diagnostics;
using System.IO;
using Atc.Data.Airports;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Atc.Data.Sources;
using Atc.Data.Sources.Embedded;
using Atc.Data.Sources.XP.Airports;
using Atc.Data.Traffic;
using Autofac;
using Zero.Doubt.Logging;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Compiler
{
    public class CompileCacheTask : ICompilerTask
    {
        private readonly ICompilerLogger _logger;
        private readonly Func<RegionDatReader> _regionDatReaderFactory;
        private readonly Func<AirlineDatReader> _airlineDatReaderFactory;
        private readonly Func<TypeJsonReader> _typeJsonReaderFactory;
        private readonly Func<RouteDatReader> _routeDatReaderFactory;
        private readonly Func<XPAptDatReader> _aptDatReaderFactory;

        public CompileCacheTask(
            ICompilerLogger logger,
            Func<RegionDatReader> regionDatReaderFactory,
            Func<AirlineDatReader> airlineDatReaderFactory,
            Func<TypeJsonReader> typeJsonReaderFactory,
            Func<RouteDatReader> routeDatReaderFactory,
            Func<XPAptDatReader> aptDatReaderFactory)
        {
            _logger = logger;
            _regionDatReaderFactory = regionDatReaderFactory;
            _airlineDatReaderFactory = airlineDatReaderFactory;
            _typeJsonReaderFactory = typeJsonReaderFactory;
            _routeDatReaderFactory = routeDatReaderFactory;
            _aptDatReaderFactory = aptDatReaderFactory;
        }

        public bool ValidateArguments(InputArguments args)
        {
            return args.Validate(xpFolderPathRequired: true);
        }

        public void Execute(InputArguments args)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(args.DataCacheFilePath)!);
            _logger.CompilingCache(outputFile: args.DataCacheFilePath);

            var clock = Stopwatch.StartNew();
            using var scope = AtcBufferContext.CreateEmpty(out var context);
            using var output = File.Create(args.DataCacheFilePath);

            
            LoadTypes(args);
            LoadRegions(args);
            LoadAirlines(args);
            LoadAirports(args);
            //LoadRoutes(args);

            ((BufferContext)context).WriteTo(output);
            output.Flush();
            _logger.CacheFileCreated(sizeMb: output.Length >> 20);
            
            PrintSummary(clock.Elapsed);
        }

        private void LoadTypes(InputArguments args)
        {
            var context = BufferContext.Current;
            ref var worldData = ref context.GetWorldData();

            var dummyFlightModelRef = context.AllocateRecord(new FlightModelData());

            worldData.TypeByIcao.Add(
                context.AllocateString("B738"),
                context.AllocateRecord(new AircraftTypeData() {
                    Icao = context.AllocateString("B738"),
                    Name = context.AllocateString("Boeing 738-800"),
                    Callsign = context.AllocateString("B738"),
                    Category = AircraftCategories.Jet,
                    Operations = OperationTypes.Airline | OperationTypes.Cargo,
                    FlightModel = dummyFlightModelRef,
                })
            );
            
            // var jsonFilePath = Path.Combine(args.AtcFolderPath, "type.json");
            // _logger.LoadingTypes(jsonFile: jsonFilePath);
            //
            // ref var worldData = ref BufferContext.Current.GetWorldData();
            //
            // var reader = _typeJsonReaderFactory();
            // using var file = File.OpenRead(jsonFilePath);
            // var allTypeRefs = reader.ReadTypeJson(file);
            //
            // foreach (var typeRef in allTypeRefs)
            // {
            //     if (!worldData.TypeByIcao.TryAdd(typeRef.Get().Icao, typeRef))
            //     {
            //         _logger.DuplicateTypeIcao(icao: typeRef.Get().Icao.GetValueNonCached());
            //     }
            // }
        }

        private void LoadRegions(InputArguments args)
        {
            var datFilePath = Path.Combine(args.AtcFolderPath, "region-code.dat");
            _logger.LoadingRegions(datFile: datFilePath);
            
            ref var worldData = ref BufferContext.Current.GetWorldData();

            var reader = _regionDatReaderFactory();
            using var file = File.OpenRead(datFilePath);
            var allRegionRefs = reader.ReadRegions(file);
            
            foreach (var regionRef in allRegionRefs)
            {
                worldData.RegionByIcao.Add(regionRef.Get().Code, regionRef);
            }
        }

        private void LoadAirlines(InputArguments args)
        {
            var datFilePath = Path.Combine(args.AtcFolderPath, "airline.dat");
            _logger.LoadingAirlines(datFile: datFilePath);

            ref var worldData = ref BufferContext.Current.GetWorldData();

            var reader = _airlineDatReaderFactory();
            using var file = File.OpenRead(datFilePath);
            var allAirlineRefs = reader.ReadAirlineDat(file);
            
            foreach (var airlineRef in allAirlineRefs)
            {
                if (!worldData.AirlineByIcao.TryAdd(airlineRef.Get().Icao, airlineRef))
                {
                    _logger.DuplicateAirlineIcao(icao: airlineRef.Get().Icao.Value);
                }
            }
        }
            
        private void LoadAirports(InputArguments args)
        {
            var aptDatFilePath = GetGlobalAptDatFilePath(args.XPFolderPath);
            _logger.LoadingAirports(datFile: aptDatFilePath);

            using var input = File.OpenRead(aptDatFilePath);//"/Users/felixb/oss/xpcpp/data/apt.dat"); // 
            var aptDatReader = _aptDatReaderFactory();
            aptDatReader.ReadAptDat(input, OnQueryAirspace, onFilterAirport: null, onAirportLoaded: null);

            ref var worldData = ref BufferContext.Current.GetWorldData();
            var buffer = BufferContext.Current.GetBuffer<AirportData>();

            for (int i = 0 ; i < buffer.RecordCount; i++)
            {
                var airportRef = buffer.GetRecordZRef(i);
                ref var airport = ref airportRef.Get();

                if (!worldData.AirportByIcao.Contains(airport.Header.Icao))
                {
                    worldData.AirportByIcao.Add(airport.Header.Icao, airportRef);
                }
                else
                {
                    _logger.DuplicateAirportIcao(icao: airport.Header.Icao.Value);
                }
            }
        }

        private void LoadRoutes(InputArguments args)
        {
            var datFilePath = Path.Combine(args.AtcFolderPath, "route.dat");
            _logger.LoadingRoutes(datFile: datFilePath);

            var reader = _routeDatReaderFactory();
            using var file = File.OpenRead(datFilePath);
            reader.ReadRouteDat(file);
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

        private void PrintSummary(TimeSpan elapsed)
        {
            ref var worldData = ref BufferContext.Current.GetWorldData();

            _logger.Success(
                time: $"{System.Math.Round(elapsed.TotalSeconds)} sec",
                airlines: worldData.AirlineByIcao.Count,
                airports: worldData.AirportByIcao.Count);
        }

        private string GetGlobalAptDatFilePath(string xpFolderPath)
        {
            return Path.Combine(
                xpFolderPath,
                "Custom Scenery",
                "Global Airports",
                "Earth nav data",
                "apt.dat"
            );
        }
    }
}

