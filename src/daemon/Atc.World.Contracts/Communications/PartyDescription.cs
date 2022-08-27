namespace Atc.World.Contracts.Communications;

public abstract class PartyDescription
{
    protected PartyDescription(string uniqueId, NatureType nature, VoiceDescription voice)
    {
        UniqueId = uniqueId;
        Voice = voice;
        Nature = nature;
    }

    public string UniqueId { get; }
    public NatureType Nature { get; }
    public VoiceDescription Voice { get; }
}

public class ServicePartyDescription : PartyDescription 
{
    public ServicePartyDescription(string uniqueId, Callsign callsign, VoiceDescription voice, AutomaticServiceType serviceType) : 
        base(uniqueId, NatureType.AI, voice)
    {
        ServiceType = serviceType;
        Callsign = callsign;
    }

    public AutomaticServiceType ServiceType { get; }  
    public Callsign Callsign { get; }
}

public class PersonPartyDescription : PartyDescription
{
    public PersonPartyDescription(
        string uniqueId, 
        NatureType nature, 
        VoiceDescription voice, 
        GenderType gender, 
        AgeType age, 
        SeniorityType seniority, 
        string? firstName) : 
        base(uniqueId, nature, voice)
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