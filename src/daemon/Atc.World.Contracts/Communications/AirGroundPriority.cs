namespace Atc.World.Contracts.Communications;

public enum AirGroundPriority
{
    None = 0,
    
    // MAYDAY
    Distress = 1, 
    
    // PAN-PAN / MEDICAL
    Urgency = 2,  
    
    DirectionFinding = 3,
    
    // AI pilots can specify 3 priority levels within Flight Safety category,
    // depending on how urgent it is to get an instruction from ATC   
    FlightSafetyHighest = 4,
    FlightSafetyHigh = 5,
    FlightSafetyNormal = 6,
    
    Meteorology = 7,

    FlightRegularity = 8
}
