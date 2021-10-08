using System;
using Atc.Data.Primitives;

namespace Atc.World.Abstractions
{
    public static class AviationDomain
    {
        public static readonly TimeSpan SilenceDurationBeforeNewConversation = TimeSpan.FromSeconds(3);
    }
}
