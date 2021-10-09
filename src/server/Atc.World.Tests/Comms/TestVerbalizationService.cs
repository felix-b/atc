using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms.Verbalizers;

namespace Atc.World.Tests.Comms
{
    public class TestVerbalizationService : IVerbalizationService
    {
        public IVerbalizer GetVerbalizer(PartyDescription speaker, LanguageCode language)
        {
            return new TestVerbalizer();
        }
                
        public class TestVerbalizer : IVerbalizer
        {
            public UtteranceDescription VerbalizeIntent(PartyDescription speaker, Intent intent)
            {
                if (intent is TestGreetingIntent greeting)
                {
                    return new UtteranceDescription(Language, new[] {
                        new UtteranceDescription.Part(
                            UtteranceDescription.PartType.Greeting, 
                            $"This is a test greeting number {greeting.RepeatCount} from {intent.Header.OriginatorCallsign}"
                        )
                    });
                }

                throw new NotImplementedException();
            }

            public LanguageCode Language { get; } = "en-US";
        }
    }
}