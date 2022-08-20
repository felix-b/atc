namespace Atc.World.Contracts.Data;

public interface IAviationDatabase
{
    IAircraftTypeData GetAircraftTypeData(string typeIcao);
    IAircraftData GetAircraftData(string tailNo);
    IEnumerable<IAircraftData> GetParkingAircraft(string airportIcao, DateTime utc);
}
