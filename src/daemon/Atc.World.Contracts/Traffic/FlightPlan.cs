using System.Collections.Immutable;
using Atc.Maths;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;

namespace Atc.World.Contracts.Traffic;

public abstract record FlightPlan(
    string TailNo,
    Callsign Callsign,
    FlightType FlightType,
    FlightRules FlightRules,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc
);

public record PatternFlightPlan(
    string TailNo,
    Callsign Callsign,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.StayInPattern, 
    FlightRules: FlightRules.Vfr,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc
);

public record TrainingAreasFlightPlan(
    string TailNo,
    Callsign Callsign,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc,
    FlightLeg? InitialLeg,
    string? TrainingAreaName
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.TrainingAreas, 
    FlightRules: FlightRules.Vfr,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc
);

public record CrossCountryFlightPlan(
    string TailNo,
    Callsign Callsign,
    FlightRules FlightRules,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc,
    string OriginIcao,
    ImmutableList<FlightLeg> Legs,
    string DestinationIcao
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.CrossCountry,
    FlightRules: FlightRules,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc
);

public record IfrFlightPlan(
    string TailNo,
    Callsign Callsign,
    string OriginIcao,
    string DestinationIcao,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc,
    ImmutableList<FlightLeg> Legs,
    string? SidName,
    string? StarName,
    string? ApproachName
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.CrossCountry,
    FlightRules: FlightRules.Ifr,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc
);

public record FlightLeg( 
    string ToWaypointName,
    Altitude Altitude,
    Bearing Track,
    Speed Speed
); 
