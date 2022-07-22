namespace Atc.World.Contracts.Communications;

public class UtteranceDescription
{
    public UtteranceDescription(
        LanguageCode language, 
        IEnumerable<Part> parts, 
        TimeSpan estimatedDuration)
    {
        Language = language;
        Parts = parts.ToArray();
        EstimatedDuration = estimatedDuration;
    }
        
    public LanguageCode Language { get; }
    public IReadOnlyList<Part> Parts { get; }
    public TimeSpan EstimatedDuration { get; }

    public bool IsEmpty => Parts.Count == 0;

    public enum PartType
    {
        None = 0,
        Text = 1,
        Callsign = 2,
        Instruction = 3,
        Data = 4,
        Disfluency = 5,
        Correction = 6,
        Punctuation = 7,
    }

    public record Part(
        PartType Type, 
        string Text 
    );

    public static UtteranceDescription Empty = new UtteranceDescription(
        new LanguageCode(), 
        Array.Empty<Part>(), 
        TimeSpan.Zero);

    public static class PartFactory
    {
        public static Part TextPart(string contents)
        {
            return new Part(PartType.Text, contents);
        }

        public static Part CallsignPart(string contents)
        {
            return new Part(PartType.Callsign, contents);
        }

        public static Part DataPart(string contents)
        {
            return new Part(PartType.Data, contents);
        }

        public static Part InstructionPart(string contents)
        {
            return new Part(PartType.Instruction, contents);
        }

        public static Part DisfluencyPart(string contents)
        {
            return new Part(PartType.Disfluency, contents);
        }

        public static Part CorrectionPart(string contents)
        {
            return new Part(PartType.Correction, contents);
        }

        public static Part PunctuationPart()
        {
            return new Part(PartType.Punctuation, string.Empty);
        }

        public static Part FarewellPart(string contents)
        {
            return new Part(PartType.Text, contents);
        }
    }
}