using System.Collections.Generic;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;

namespace Atc.World.Tests.Comms
{
    internal class TestSpeaker : PersonParty
    {
        public TestSpeaker(string callsign) : 
            base("test/#123", callsign, NatureType.AI, VoiceDescription.Default, GenderType.Male, AgeType.Mature, firstName: null)
        {
        }

        public override void ReceiveIntent(Intent intent)
        {
            ReceivedIntents.Add(intent);
        }

        public List<Intent> ReceivedIntents { get; } = new List<Intent>();
    }
}