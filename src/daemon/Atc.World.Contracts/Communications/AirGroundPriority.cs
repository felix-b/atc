namespace Atc.World.Contracts.Communications;

public enum AirGroundPriority
{
    None = 0,
    Distress = 1, // MAYDAY
    Urgency = 2,  // PAN-PAN / MEDICAL
    DirectionFinding = 3,
    FlightSafetyHighest = 4,
    FlightSafetyHigh = 5,
    FlightSafetyNormal = 6,
    Meteorology = 7,
    FlightRegularity = 8
}
