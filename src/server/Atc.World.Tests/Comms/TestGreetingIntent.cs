using System.Security.Cryptography.X509Certificates;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Tests.Comms
{
    internal record TestGreetingIntent : Intent
    {
        public TestGreetingIntent(IWorldContext world, int repeatCount, ActorRef<RadioStationActor> from, ActorRef<RadioStationActor>? to = null) 
            : base(new IntentHeader(
                WellKnownIntentType.Greeting, 
                CustomCode: 0, 
                OriginatorUniqueId: from.UniqueId,
                OriginatorCallsign: from.Get().Callsign,
                RecipientUniqueId: to?.UniqueId,
                RecipientCallsign: to?.Get().Callsign,
                CreatedAtUtc: world.UtcNow()))
        {
            RepeatCount = repeatCount;
        }
        
        public int RepeatCount { get; }
    }
}
