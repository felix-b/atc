namespace Atc.World.Contracts.Communications;

public enum AirGroundPriority
{
    GroundToAir = 0,
    
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

    FlightRegularity = 8,
}

public static class AirGroundPriorityExtensions
{
    public static TimeSpan RequiredSilenceBeforeNewConversation(this AirGroundPriority value)
    {
        switch (value)
        {
            case AirGroundPriority.GroundToAir:
                return TimeSpan.Zero; 
            case AirGroundPriority.Distress:
            case AirGroundPriority.Urgency:
                return TimeSpan.Zero; 
            case AirGroundPriority.DirectionFinding:
            case AirGroundPriority.FlightSafetyHighest:
                return TimeSpan.FromMilliseconds(500); 
            case AirGroundPriority.FlightSafetyHigh:
                return TimeSpan.FromMilliseconds(750); 
            case AirGroundPriority.FlightSafetyNormal:
                return TimeSpan.FromMilliseconds(1000); 
            case AirGroundPriority.Meteorology:
                return TimeSpan.FromMilliseconds(1500); 
            case AirGroundPriority.FlightRegularity:
                return TimeSpan.FromMilliseconds(2000); 
            default:
                return TimeSpan.FromSeconds(3);
        }
    }
}