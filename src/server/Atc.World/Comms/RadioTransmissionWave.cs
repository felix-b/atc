using System;
using Atc.Data.Primitives;
using Atc.World.Abstractions;

namespace Atc.World.Comms
{
    public class RadioTransmissionWave
    {
        private readonly Intent _intent;

        public RadioTransmissionWave(LanguageCode language, byte[] bytes, TimeSpan duration, Intent intent)
        {
            Bytes = bytes;
            Duration = duration;
            Language = language;
            _intent = intent;
        }

        public Intent? TryGetIntent()
        {
            return _intent;
        }

        public Intent GetIntentOrThrow()
        {
            return _intent;
        }
        
        public readonly byte[] Bytes;
        public readonly TimeSpan Duration;
        public readonly LanguageCode Language;
        
        public bool HasBytes => false;
    }
}
