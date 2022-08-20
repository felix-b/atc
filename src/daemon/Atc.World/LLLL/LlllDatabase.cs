using Atc.World.Contracts.Data;

namespace Atc.World.LLLL;

public class LlllDatabase : IAviationDatabase
{
    public IAircraftTypeData GetAircraftTypeData(string typeIcao)
    {
        return new AircraftTypeData(
            Icao: typeIcao,
            FullName: "Cessna 172",
            Categories: AircraftCategories.Prop,
            Operations: AirOperationTypes.GA);
    }

    public IAircraftData GetAircraftData(string tailNo)
    {
        return new AircraftData(
            TypeIcao: "C172", 
            TailNo: tailNo);
    }

    public IEnumerable<IAircraftData> GetParkingAircraft(string airportIcao, DateTime utc)
    {
        var tailNos = new[] {
            "4XCGK", 
            "4XCDK",
            "4XCDC",
            "4XCDT"
        };

        return tailNos.Select(GetAircraftData);
    }

    private record AircraftData(
        string TypeIcao,
        string TailNo
    ) : IAircraftData;

    private record AircraftTypeData(
        string Icao,
        string FullName,
        AircraftCategories Categories,
        AirOperationTypes Operations
    ) : IAircraftTypeData;
}
