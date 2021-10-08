using System;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;

namespace Atc.World.Comms.Verbalizers
{
    public class USEnglishVerbalizer : IVerbalizer
    {
        public UtteranceDescription VerbalizeIntent(Party speaker, Intent intent)
        {
            if (intent.Header.Type == WellKnownIntentType.Greeting)
            {
                return new UtteranceDescription(Language, new[] {
                    new UtteranceDescription.Part(
                        UtteranceDescription.PartType.Greeting, 
                        $"This is a test greeting from {intent.Header.OriginatorCallsign}"
                    )
                });
            }

            throw new NotImplementedException();
        }

        public LanguageCode Language { get; } = "en-US";
    }
}