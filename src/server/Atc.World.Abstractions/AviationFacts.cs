using System;

namespace Atc.World.Abstractions
{
    public static class AviationFacts
    {
        public static readonly TimeSpan SilenceDurationBeforeNewConversation = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan SilenceDurationBeforeReadback = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan ControllerRadarRefreshRate = TimeSpan.FromSeconds(5);
    }
}
