using System;
using Atc.World.Abstractions;

namespace Atc.World.Comms
{
    public class RuntimeWave
    {
        private readonly Intent _intent;

        public RuntimeWave(byte[] bytes, TimeSpan duration, Intent intent)
        {
            Bytes = bytes;
            Duration = duration;
            _intent = intent;
        }

        public Intent? TryGetIntent()
        {
            return _intent;
        }
        
        public readonly byte[] Bytes;
        public readonly TimeSpan Duration;
    }
}
