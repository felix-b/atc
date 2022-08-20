using System.Collections.Immutable;
using Atc.Maths;

namespace Atc.World.Contracts.Airports;

public record TerminalInformation(
    string Icao, // Aerodrome ICAO code
    string Designator, // letter A-Z
    Wind Wind,
    Pressure Qnh,
    ImmutableList<string> ActiveRunwaysDeparture, 
    ImmutableList<string> ActiveRunwaysArrival 
    //TODO: add clouds & visibility
    //TODO: add NOTAMs
) {
    public string ActiveRunwaysDepartureCommaSeparated => string.Join(", ", ActiveRunwaysDeparture);
    public string ActiveRunwaysArrivalCommaSeparated => string.Join(", ", ActiveRunwaysArrival);
}
