namespace Atc.World.Contracts.Communications;

public abstract class PartyDescription
{
    protected PartyDescription(string uniqueId, Callsign callsign, NatureType nature, VoiceDescription voice)
    {
        UniqueId = uniqueId;
        Callsign = callsign;
        Voice = voice;
        Nature = nature;
    }

    public string UniqueId { get; }
    public Callsign Callsign { get; }
    public NatureType Nature { get; }
    public VoiceDescription Voice { get; }
}

public class ServicePartyDescription : PartyDescription 
{
    public ServicePartyDescription(string uniqueId, Callsign callsign, VoiceDescription voice, AutomaticServiceType serviceType) : 
        base(uniqueId, callsign, NatureType.AI, voice)
    {
        ServiceType = serviceType;
    }

    public AutomaticServiceType ServiceType { get; }  
}

public class PersonPartyDescription : PartyDescription
{
    public PersonPartyDescription(
        string uniqueId, 
        Callsign callsign, 
        NatureType nature, 
        VoiceDescription voice, 
        GenderType gender, 
        AgeType age, 
        SeniorityType seniority, 
        string? firstName) : 
        base(uniqueId, callsign, nature, voice)
    {
        Gender = gender;
        Age = age;
        Seniority = seniority;
        FirstName = firstName;
    }

    public GenderType Gender { get; }
    public AgeType Age { get; }
    public SeniorityType Seniority { get; }
    public string? FirstName { get; }
}

public enum AutomaticServiceType
{
    Atis,
    Awos
}
    
public enum GenderType
{
    Male,
    Female
}

public enum AgeType
{
    Young,
    Mature,
    Senior
}

public enum SeniorityType
{
    Novice,
    Senior,
    Veteran
}

public enum NatureType
{
    Human = 0,
    AI = 1
}
