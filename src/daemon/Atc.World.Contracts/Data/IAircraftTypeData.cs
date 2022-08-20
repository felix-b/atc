namespace Atc.World.Contracts.Data;

public interface IAircraftTypeData
{
    string Icao { get; }
    string FullName { get; }
    AircraftCategories Categories { get; }
    AirOperationTypes Operations { get; }
}
