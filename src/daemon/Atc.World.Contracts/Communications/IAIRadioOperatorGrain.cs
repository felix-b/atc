using Atc.Grains;

namespace Atc.World.Contracts.Communications;

public interface IAIRadioOperatorGrain : IGrainId
{
    // Called when it's the AI operator's turn to transmit,
    // according to priority queue managed by GroundStationRadioMediumGrain.
    // At this moment the AI operator can either start transmitting
    // by invoking associated IRadioStationGrain.BeginTransmission,
    // or give up the transmission. The returned response must match the action taken.
    // If BeganTransmission == false is returned, the AI operator is removed from the queue,
    // and it has to call EnqueueAIOperatorForTransmission again.
    BeginTransmitNowResponse BeginTransmitNow(ConversationToken conversationToken);

    // Called when speech synthesis is complete for the transmission,
    // and the exact duration of speech is known. 
    // This allows AI operator to correctly time the end of transmission.
    void NotifyTransmissionDurationAvailable(ulong transmissionId, DateTime startUtc, TimeSpan duration);
    
    PartyDescription Party { get; }
}

public record BeginTransmitNowResponse(
    // whether the AI operator began a transmission
    bool BeganTransmission,
    // conversation token associated with the started transmission
    // must be present if BeganTransmission is true, otherwise it must be null
    ConversationToken? ConversationToken = null,
    // optionally, update priority of the conversation
    // must be null if BeganTransmission is false.
    AirGroundPriority? NewPriority = null
);
