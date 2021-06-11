namespace Atc.Data.Sources.Embedded
{
    public interface IEmbeddedDataSourcesLogger
    {
        void IcaoRegionCodeError(string line);
        void IcaoAirlineError(string line);
        void DoneReadingAirlines(int loaded, int skippedCount, int wereFixed);
    }
}