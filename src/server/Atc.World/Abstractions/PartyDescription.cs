using Atc.Speech.Abstractions;

namespace Atc.World.Abstractions
{
    public abstract class PartyDescription
    {
        protected PartyDescription(string uniqueId, string callsign, NatureType nature, VoiceDescription voice)
        {
            UniqueId = uniqueId;
            Callsign = callsign;
            Voice = voice;
            Nature = nature;
        }

        public string UniqueId { get; }
        public string Callsign { get; }
        public NatureType Nature { get; }
        public VoiceDescription Voice { get; }
    }

    public interface IHaveParty
    {
        PartyDescription Party { get; }
    }
    
    public class AutomaticServiceDescription : PartyDescription 
    {
        public AutomaticServiceDescription(string uniqueId, string callsign, VoiceDescription voice, AutomaticServiceType serviceType) : 
            base(uniqueId, callsign, NatureType.AI, voice)
        {
            ServiceType = serviceType;
        }

        public AutomaticServiceType ServiceType { get; }  
    }

    public class PersonDescription : PartyDescription
    {
        public PersonDescription(
            string uniqueId, 
            string callsign, 
            NatureType nature, 
            VoiceDescription voice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(uniqueId, callsign, nature, voice)
        {
            Gender = gender;
            Age = age;
            FirstName = firstName;
        }

        public GenderType Gender { get; }
        public AgeType Age { get; }
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

    public enum NatureType
    {
        Human = 0,
        AI = 1
    }
}
