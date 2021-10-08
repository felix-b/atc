using Atc.Speech.Abstractions;

namespace Atc.World.Abstractions
{
    public abstract class Party
    {
        protected Party(string uniqueId, string callsign, NatureType nature, VoiceDescription defaultVoice)
        {
            UniqueId = uniqueId;
            Callsign = callsign;
            DefaultVoice = defaultVoice;
            Nature = nature;
        }

        public string UniqueId { get; }
        public string Callsign { get; }
        public NatureType Nature { get; }
        public VoiceDescription DefaultVoice { get; }

        public abstract void ReceiveIntent(Intent intent);
    }

    public interface IHaveParty
    {
        Party Party { get; }
    }
    
    public abstract class AutomaticStationParty : Party 
    {
        protected AutomaticStationParty(string uniqueId, string callsign, VoiceDescription defaultVoice, AutomaticStationType stationType) : 
            base(uniqueId, callsign, NatureType.AI, defaultVoice)
        {
            StationType = stationType;
        }

        public AutomaticStationType StationType { get; }  
    }

    public abstract class PersonParty : Party
    {
        protected PersonParty(
            string uniqueId, 
            string callsign, 
            NatureType nature, 
            VoiceDescription defaultVoice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(uniqueId, callsign, nature, defaultVoice)
        {
            Gender = gender;
            Age = age;
            FirstName = firstName;
        }

        public GenderType Gender { get; }
        public AgeType Age { get; }
        public string? FirstName { get; }
    }

    public abstract class PilotParty : PersonParty
    {
        protected PilotParty(
            string uniqueId, 
            string callsign, 
            NatureType nature, 
            VoiceDescription defaultVoice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(uniqueId, callsign, nature, defaultVoice, gender, age, firstName)
        {
        }
    }

    public abstract class ControllerParty : PersonParty 
    {
        protected ControllerParty(
            string uniqueId, 
            string callsign, 
            NatureType nature, 
            VoiceDescription defaultVoice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(uniqueId, callsign, nature, defaultVoice, gender, age, firstName)
        {
        }
    }

    public enum AutomaticStationType
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
