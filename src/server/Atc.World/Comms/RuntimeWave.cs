using System;

namespace Atc.World.Comms
{
    public class RuntimeWave
    {
        public RuntimeWave(byte[] bytes, TimeSpan duration)
        {
            Bytes = bytes;
            Duration = duration;
        }

        public readonly byte[] Bytes;
        public readonly TimeSpan Duration;
    }
}