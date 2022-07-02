namespace Atc.World.Contracts.Communications;

public enum TransceiverStatus
{
    NoMedium,
    Silence,
    ReceivingSingleTransmission,
    ReceivingMutualCancellation,
    Transmitting
}
