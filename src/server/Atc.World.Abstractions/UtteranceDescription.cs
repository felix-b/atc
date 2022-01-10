using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Primitives;

namespace Atc.World.Abstractions
{
    public class UtteranceDescription
    {
        public UtteranceDescription(
            LanguageCode language, 
            IEnumerable<Part> parts, 
            TimeSpan? estimatedDuration = null) //TODO: temporary, should be required
        {
            Language = language;
            Parts = parts.ToArray();
            EstimatedDuration = estimatedDuration ?? TimeSpan.FromSeconds(5);
        }
        
        public LanguageCode Language { get; }
        public IReadOnlyList<Part> Parts { get; }
        public TimeSpan EstimatedDuration { get; }

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

        public enum IntonationType
        {
            Neutral = 0,
            Greeting = 1,
            Farewell = 2,
            Affirmation = 3,
            Negation = 4,
        }
        
        public record Part(
            PartType Type, 
            string Text, 
            IntonationType? Intonation = IntonationType.Neutral
        );

        public static class PartFactory
        {
            public static Part TextPart(string contents, IntonationType? intonation = IntonationType.Neutral)
            {
                return new Part(PartType.Text, contents, intonation);
            }

            public static Part CallsignPart(string contents, IntonationType? intonation = IntonationType.Neutral)
            {
                return new Part(PartType.Callsign, contents, intonation);
            }

            public static Part DataPart(string contents, IntonationType? intonation = IntonationType.Neutral)
            {
                return new Part(PartType.Data, contents, intonation);
            }

            public static Part InstructionPart(string contents, IntonationType? intonation = IntonationType.Neutral)
            {
                return new Part(PartType.Instruction, contents, intonation);
            }

            public static Part DisfluencyPart(string contents, IntonationType? intonation = IntonationType.Neutral)
            {
                return new Part(PartType.Disfluency, contents, intonation);
            }

            public static Part CorrectionPart(string contents, IntonationType? intonation = IntonationType.Neutral)
            {
                return new Part(PartType.Correction, contents, intonation);
            }

            public static Part PunctuationPart()
            {
                return new Part(PartType.Punctuation, string.Empty);
            }

            public static Part FarewellPart(string contents)
            {
                return new Part(PartType.Text, contents, IntonationType.Farewell);
            }
        }
    }
}
