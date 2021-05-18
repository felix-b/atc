namespace Atc.Data.Control
{
    public enum ControllerPositionType
    {
        Unknown = 0,
        FlightData = 1,
        ClearanceDelivery = 2,
        Ground = 3,
        Local = 4,
        Departure = 5,            
        Approach = 6,
        Area = 7,
        Oceanic = 8
    }
    
    public enum ControlFacilityType
    {
        Unknown = 0,
        Tower = 1,
        Terminal = 2,
        Center = 3,
        Oceanic = 4
    }
}
