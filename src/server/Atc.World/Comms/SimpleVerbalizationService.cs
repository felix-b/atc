using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms.Verbalizers;

namespace Atc.World.Comms
{
    public class SimpleVerbalizationService : IVerbalizationService
    {
        private static readonly Dictionary<LanguageCode, IVerbalizer> __verbalizerByLanguageCode =
            new Dictionary<LanguageCode, IVerbalizer>() {
                { "en-US", new USEnglishVerbalizer() }
            };
        
        public IVerbalizer GetVerbalizer(PartyDescription speaker, LanguageCode? language = null)
        {
            var effectiveLanguage = language ?? speaker.Voice.Language;
            return __verbalizerByLanguageCode[effectiveLanguage];
        }
    }
}
