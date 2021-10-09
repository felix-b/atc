namespace Atc.World.Comms
{
    public enum TransceiverStatus
    {
        NoReachableAether,
        DetectingSilence,
        Silence,
        ReceivingSingleTransmission,
        ReceivingMutualCancellation,
        Transmitting
    }
}