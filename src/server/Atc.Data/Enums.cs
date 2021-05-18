using System;

namespace Atc.Data
{
    [Flags]
    public enum WeekDays
    {
        Sunday = 0x01,
        Monday = 0x02,
        Tuesday = 0x04,
        Wednesday = 0x08,
        Thursday = 0x10,
        Friday = 0x20,
        Saturday = 0x40
    }
    
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
    public enum OperationTypes
    {
        None = 0x0,
        GA = 0x01,
        Airline = 0x02,
        Cargo = 0x04,
        Military = 0x08,
    }
}
