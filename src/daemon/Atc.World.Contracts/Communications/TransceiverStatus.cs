namespace Atc.World.Contracts.Communications;

public enum TransceiverStatus
{
    Off,
    NoMedium,
    Silence,
    ReceivingSingleTransmission,
    ReceivingInterferenceNoise,
    Transmitting
}
