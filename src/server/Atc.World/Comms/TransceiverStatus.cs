namespace Atc.World.Comms
{
    public enum TransceiverStatus
    {
        DetectingSilence,
        Silence,
        ReceivingSingleTransmission,
        ReceivingMutualCancellation,
        Transmitting
    }
}