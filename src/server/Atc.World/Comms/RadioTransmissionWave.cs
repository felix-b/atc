using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Abstractions;

namespace Atc.World.Comms
{
    public record RadioTransmissionWave(
        UtteranceDescription? Utterance,
        VoiceDescription? Voice,
        ImmutableList<ImmutableArray<byte>>? SoundBuffers)
    {
        public bool HasSoundStream => SoundBuffers != null;
    }
}
