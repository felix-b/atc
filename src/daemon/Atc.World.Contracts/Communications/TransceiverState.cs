namespace Atc.World.Contracts.Communications;

public record TransceiverState(
    TransceiverStatus Status,
    TransmissionDescription? Transmission, // when Transmitting or ReceivingSingleTransmission
    ConversationToken? ConversationToken,
    bool PttPressed
);
