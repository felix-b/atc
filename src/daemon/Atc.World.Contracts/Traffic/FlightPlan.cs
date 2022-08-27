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
    string OriginIcao,
    string DestinationIcao,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc,
    ImmutableList<FlightLeg> Legs
);

public record PatternFlightPlan(
    string TailNo,
    Callsign Callsign,
    string OriginIcao,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.StayInPattern, 
    FlightRules: FlightRules.Vfr,
    OriginIcao: OriginIcao,
    DestinationIcao: OriginIcao,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc,
    Legs: ImmutableList<FlightLeg>.Empty
);

public record TrainingAreasFlightPlan(
    string TailNo,
    Callsign Callsign,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc,
    string OriginIcao,
    string? TrainingAreaName,
    ImmutableList<FlightLeg> Legs
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.TrainingAreas, 
    FlightRules: FlightRules.Vfr,
    OriginIcao: OriginIcao,
    DestinationIcao: OriginIcao,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc,
    Legs: Legs
);

public record CrossCountryFlightPlan(
    string TailNo,
    Callsign Callsign,
    FlightRules FlightRules,
    DateTime TakeoffTimeUtc,
    DateTime LandingTimeUtc,
    string OriginIcao,
    string DestinationIcao,
    ImmutableList<FlightLeg> Legs
) : FlightPlan(
    TailNo: TailNo,
    Callsign: Callsign,
    FlightType: FlightType.CrossCountry,
    FlightRules: FlightRules,
    OriginIcao: OriginIcao,
    DestinationIcao: DestinationIcao,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc,
    Legs: Legs
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
    OriginIcao: OriginIcao,
    DestinationIcao: DestinationIcao,
    TakeoffTimeUtc: TakeoffTimeUtc,
    LandingTimeUtc: LandingTimeUtc,
    Legs: Legs
);

public record FlightLeg( 
    string ToWaypointName,
    Altitude Altitude,
    Bearing Track,
    Speed Speed
); 
