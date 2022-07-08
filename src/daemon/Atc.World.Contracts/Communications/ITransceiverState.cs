using Atc.Maths;

namespace Atc.World.Contracts.Communications;

public interface ITransceiverState
{
    Frequency SelectedFrequency { get; }

    bool PttPressed { get; }

    TransceiverStatus Status { get; }

    // when Status is either Transmitting or ReceivingSingleTransmission
    TransmissionDescription? CurrentTransmission { get; }

    ConversationToken? ConversationToken { get; }
}
