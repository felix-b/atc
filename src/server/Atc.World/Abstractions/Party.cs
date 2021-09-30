using Atc.Speech.Abstractions;

namespace Atc.World.Abstractions
{
    public abstract class Party
    {
        protected Party(int id, string callsign, NatureType nature, VoiceDescription defaultVoice)
        {
            Id = id;
            Callsign = callsign;
            DefaultVoice = defaultVoice;
            Nature = nature;
        }

        public int Id { get; }
        public string Callsign { get; }
        public NatureType Nature { get; }
        public VoiceDescription DefaultVoice { get; }

        public abstract void ReceiveIntent(Intent intent);
    }

    public abstract class AutomaticStation : Party 
    {
        protected AutomaticStation(int id, string callsign, VoiceDescription defaultVoice, AutomaticStationType stationType) : 
            base(id, callsign, NatureType.AI, defaultVoice)
        {
            StationType = stationType;
        }

        public AutomaticStationType StationType { get; }  
    }

    public abstract class Person : Party
    {
        protected Person(
            int id, 
            string callsign, 
            NatureType nature, 
            VoiceDescription defaultVoice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(id, callsign, nature, defaultVoice)
        {
            Gender = gender;
            Age = age;
            FirstName = firstName;
        }

        public GenderType Gender { get; }
        public AgeType Age { get; }
        public string? FirstName { get; }
    }

    public abstract class Pilot : Person
    {
        protected Pilot(
            int id, 
            string callsign, 
            NatureType nature, 
            VoiceDescription defaultVoice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(id, callsign, nature, defaultVoice, gender, age, firstName)
        {
        }
    }

    public abstract class Controller : Person 
    {
        protected Controller(
            int id, 
            string callsign, 
            NatureType nature, 
            VoiceDescription defaultVoice, 
            GenderType gender, 
            AgeType age, 
            string? firstName) : 
            base(id, callsign, nature, defaultVoice, gender, age, firstName)
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
