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
            Text = 0,
            Greeting = 1,
            Farewell = 2,
            Data = 3,
            Instruction = 4,
            Disfluency = 5,
            Correction = 6,
            Punctuation = 7,
            Affirmation = 8,
            Negation = 9
        }

        public record Part(PartType Type, string Text);
    }
}
