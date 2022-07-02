namespace Atc.World.Contracts.Communications;

public record TransceiverState(
    TransceiverStatus Status,
    TransmissionDescription? Transmission,
    IntentDescription? Intent,
    bool PttPressed
);
