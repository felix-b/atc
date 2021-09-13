using Zero.Serialization.Buffers;

namespace Atc.Data.Compiler
{
    public interface ICompilerLogger
    {
        void CompilingCache(string outputFile);
        void LoadingRegions(string datFile);
        void LoadingAirlines(string datFile);
        void LoadingAirports(string datFile);
        void LoadingTypes(string jsonFile);
        void LoadingRoutes(string datFile);
        void CacheFileCreated(long sizeMb);
        void Success(string time, int airlines, int airports);
        void DuplicateTypeIcao(string icao);
        void DuplicateAirlineIcao(string icao);
        void DuplicateAirportIcao(string icao);
    }
}
