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
            Disfluency = 4,
            Correction = 5,
            Punctuation = 6,
            Affirmation = 7,
            Negation = 8
        }

        public record Part(PartType Type, string Text);
    }
}
