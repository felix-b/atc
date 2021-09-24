using Atc.Data.Primitives;
using Atc.World.Abstractions;

namespace Atc.World.Comms
{
    public enum DepartureIntentType
    {
        Unspecified = 0,
        ToDestination = 0x01,
        ToStayInPattern = 0x02,
        ToTrainingZones = 0x03,
    }

    public record Atis (
        Bearing WindDirection,
        Speed WindSpeed,
        Pressure Qnh,
        string[] ActiveRunwaysDeparture,
        string[] ActiveRunwaysArrival
        //TODO: add clouds
        //TODO: add NOTAMs
    );

    public record VfrClearance(
        string? Navaid,
        Bearing? Heading,
        Altitude? Altitude
    );

    public record GreetingIntent(IntentHeader Header) : Intent(Header);
    public record GoAheadInstruction(IntentHeader Header) : Intent(Header);
    public record StartupRequestIntent(
        IntentHeader Header,
        DepartureIntentType DepartureType,
        string? DestinationIcao
    ) : Intent(Header);

    public record StartupApproval(
        IntentHeader Header,
        VfrClearance? VfrClearance  
    ) : Intent(Header);

    public record StartupApprovalReadback(
        IntentHeader Header,
        StartupApproval Approval
    ) : Intent(Header);
}
