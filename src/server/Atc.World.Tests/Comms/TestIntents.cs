using System.Security.Cryptography.X509Certificates;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Tests.Comms
{
    internal record TestGreetingIntent : Intent
    {
        public TestGreetingIntent(IWorldContext world, int repeatCount, ActorRef<RadioStationActor> from, ActorRef<RadioStationActor>? to = null) 
            : base(
                new IntentHeader(
                    WellKnownIntentType.Greeting, 
                    CustomCode: 0, 
                    OriginatorUniqueId: from.UniqueId,
                    OriginatorCallsign: from.Get().Callsign,
                    RecipientUniqueId: to?.UniqueId,
                    RecipientCallsign: to?.Get().Callsign,
                    CreatedAtUtc: world.UtcNow()),
                IntentOptions.Default)
        {
            RepeatCount = repeatCount;
        }

        public TestGreetingIntent(IWorldContext world, int repeatCount, IStatefulActor fromPartyActor, ActorRef<RadioStationActor>? to = null) 
            : base(
                new IntentHeader(
                    WellKnownIntentType.Greeting, 
                    CustomCode: 0, 
                    OriginatorUniqueId: fromPartyActor.UniqueId,
                    OriginatorCallsign: ((IHaveParty)fromPartyActor).Party.Callsign,
                    RecipientUniqueId: to?.UniqueId,
                    RecipientCallsign: to?.Get().Callsign,
                    CreatedAtUtc: world.UtcNow()),
                IntentOptions.Default)
        {
            RepeatCount = repeatCount;
        }

        public int RepeatCount { get; }
    }

    internal record TestPingIntent : Intent
    {
        public const int IntentCode = 100;
        
        public TestPingIntent(IWorldContext world, int repeatCount, ActorRef<RadioStationActor> from, ActorRef<RadioStationActor>? to = null) 
            : base(
                new IntentHeader(
                    WellKnownIntentType.Custom, 
                    CustomCode: IntentCode, 
                    OriginatorUniqueId: from.UniqueId,
                    OriginatorCallsign: from.Get().Callsign,
                    RecipientUniqueId: to?.UniqueId,
                    RecipientCallsign: to?.Get().Callsign,
                    CreatedAtUtc: world.UtcNow()),
                IntentOptions.Default)
        {
            RepeatCount = repeatCount;
        }

        public TestPingIntent(IWorldContext world, int repeatCount, IStatefulActor fromPartyActor, ActorRef<RadioStationActor>? to = null) 
            : base(
                new IntentHeader(
                    WellKnownIntentType.Custom, 
                    CustomCode: IntentCode, 
                    OriginatorUniqueId: fromPartyActor.UniqueId,
                    OriginatorCallsign: ((IHaveParty)fromPartyActor).Party.Callsign,
                    RecipientUniqueId: to?.UniqueId,
                    RecipientCallsign: to?.Get().Callsign,
                    CreatedAtUtc: world.UtcNow()),
                IntentOptions.Default)
        {
            RepeatCount = repeatCount;
        }

        public int RepeatCount { get; init; }

        public override string ToString()
        {
            return $"PING#{RepeatCount}";
        }
    }

    internal record TestPongIntent : Intent
    {
        public const int IntentCode = 101;
        
        public TestPongIntent(IWorldContext world, int repeatCount, ActorRef<RadioStationActor> from, ActorRef<RadioStationActor>? to = null) 
            : base(
                new IntentHeader(
                    WellKnownIntentType.Custom, 
                    CustomCode: IntentCode, 
                    OriginatorUniqueId: from.UniqueId,
                    OriginatorCallsign: from.Get().Callsign,
                    RecipientUniqueId: to?.UniqueId,
                    RecipientCallsign: to?.Get().Callsign,
                    CreatedAtUtc: world.UtcNow()),
                IntentOptions.Default)
        {
            RepeatCount = repeatCount;
        }

        public TestPongIntent(IWorldContext world, int repeatCount, IStatefulActor fromPartyActor, ActorRef<RadioStationActor>? to = null) 
            : base(
                new IntentHeader(
                    WellKnownIntentType.Custom, 
                    CustomCode: IntentCode, 
                    OriginatorUniqueId: fromPartyActor.UniqueId,
                    OriginatorCallsign: ((IHaveParty)fromPartyActor).Party.Callsign,
                    RecipientUniqueId: to?.UniqueId,
                    RecipientCallsign: to?.Get().Callsign,
                    CreatedAtUtc: world.UtcNow()),
                IntentOptions.Default)
        {
            RepeatCount = repeatCount;
        }

        public int RepeatCount { get; init; }

        public override string ToString()
        {
            return $"PONG#{RepeatCount}";
        }
    }
}
