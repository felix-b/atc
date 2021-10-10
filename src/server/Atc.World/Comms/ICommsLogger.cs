using System;
using Atc.World.Abstractions;
using Zero.Doubt.Logging;

namespace Atc.World.Comms
{
    public interface ICommsLogger
    {
        void StationPoweringOn(string station);
        void StationTuningTo(string station, int khz);
        void AetherStationAdded(string aether, string station);
        void AetherStationRemoved(string aether, string station);
        void RegisteredPendingTransmission(ulong tokenId, string speaker, int cookie);
        LogWriter.LogSpan InvokingListener(string uniqueId, ulong listenerId);
        LogWriter.LogSpan InvokeAllListeners(string uniqueId, WellKnownIntentType? intentType);
        LogWriter.LogSpan SynthesizingSpeech(ulong transmissionId, string fromCallsign, string utterance);
        InvalidOperationException CannotSynthesizeSpeechNoUttteranceOrVoice(ulong transmissionId, string stationId);
    }
}
