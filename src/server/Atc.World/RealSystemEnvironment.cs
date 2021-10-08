using System;
using Atc.World.Abstractions;

namespace Atc.World
{
    public class RealSystemEnvironment : ISystemEnvironment
    {
        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}