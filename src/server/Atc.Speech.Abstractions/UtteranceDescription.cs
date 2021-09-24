using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Atc.Speech.Abstractions
{
    public class UtteranceDescription
    {
        public UtteranceDescription(CultureInfo culture, IEnumerable<Part> parts)
        {
            Culture = culture;
            Parts = parts.ToArray();
        }
        
        public CultureInfo Culture { get; }
        public IReadOnlyList<Part> Parts { get; }

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
