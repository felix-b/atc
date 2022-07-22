using Atc.World.Communications;

namespace Atc.Telemetry.Impl;

public static class RadioStationSoundMonitor_Telemetry
{
    public class Noop : RadioStationSoundMonitor.IThisTelemetry
    {
        public void DebugPreparingToPlayTransmissionSpeech(ulong transmissionId)
        {
        }

        public void VerbosePlayingTransmissionSpeech(ulong transmissionId, ulong audioStreamId, TimeSpan startPoint, TimeSpan? duration)
        {
        }

        public void ErrorFailedToPlayTransmissionSpeech(ulong transmissionId, Exception exception)
        {
        }
    }
}