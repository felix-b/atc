namespace Atc.World.Contracts.Data;

[Flags]
public enum AircraftCategories
{
    None = 0x0,
    Heavy = 0x01,
    Jet = 0x02,
    Turboprop = 0x04,
    Prop = 0x08,
    LightProp = 0x10,
    Helicopter = 0x20,
    Fighter = 0x40,
    All = Heavy | Jet | Turboprop | Prop | LightProp | Helicopter | Fighter
}
    
[Flags]
public enum AirOperationTypes
{
    None = 0x0,
    GA = 0x01,
    Airline = 0x02,
    Cargo = 0x04,
    Military = 0x08,
}

public enum FlightType
{
    StayInPattern,
    TrainingAreas,
    CrossCountry
}

public enum FlightRules
{
    Vfr,
    Ifr
}

public enum ApproachType
{
    Visual,
    Ils,
    Rnp,
    NonPrecision //TODO??
}
