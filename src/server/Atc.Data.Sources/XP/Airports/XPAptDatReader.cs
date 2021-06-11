using System.IO;
using Atc.Data.Airports;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Sources.XP.Airports
{
    public class XPAptDatReader
    {
        private readonly ILogger _logger;

        public delegate bool AirportLoadedCallback(ZRef<AirportData> airport);

        public XPAptDatReader(ILogger logger)
        {
            _logger = logger;
        }

        public void ReadAptDat(Stream input,
            XPAirportReader.QueryAirspaceCallback? onQueryAirspace,
            XPAirportReader.FilterAirportCallback? onFilterAirport,
            AirportLoadedCallback? onAirportLoaded)
        {
            var inputReader = CreateAptDatStreamReader(input);
            var bufferContext = BufferContext.Current;
            
            int loadedCount = 0;
            int skippedCount = 0;
            int unparsedLineCode = -1;

            do {
                var airportReader = new XPAirportReader(
                    bufferContext, 
                    _logger, 
                    onQueryAirspace, 
                    onFilterAirport, 
                    unparsedLineCode);
                
                airportReader.ReadAirport(inputReader);
                unparsedLineCode = airportReader.UnparsedLineCode;

                var airport = airportReader.GetAirport();
                if (airport.HasValue)
                {
                    //_logger.LoadedAirport(icao: airport.Value.Get().Header.Icao.GetValueNonCached());
                    onAirportLoaded?.Invoke(airport.Value);
                    loadedCount++;
                }
                else if (airportReader.IsLandAirport)
                {
                    _logger.SkippedAirport(airportReader.Icao);
                    skippedCount++;
                }
            } while (XPAirportReader.IsAirportHeaderLineCode(unparsedLineCode));

            _logger.DoneLoadingAirports(loadedCount, skippedCount);
        }

        public static StreamReader CreateAptDatStreamReader(Stream aptDatStream)
        {
            return new StreamReader(aptDatStream, bufferSize: 4096, leaveOpen: true);
        }
        
        public interface ILogger
        {
            void LoadedAirport(string icao);
            void SkippedAirport(string icao);
            void DoneLoadingAirports(int loaded, int skipped);
            void FailedToAssembleAirport(string icao, string message);
            void FailedToLoadAirportSkipping(string icao, string diagnostic);
            void SkippingAirportByFilter(string icao);
            void WarnGateAirlineNotFound(string icao);
        }
    }
}
