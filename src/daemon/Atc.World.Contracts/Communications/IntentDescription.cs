namespace Atc.World.Contracts.Communications;

public record IntentDescription(
    ulong Id,
    WellKnownIntentType WellKnownType = WellKnownIntentType.None,
    bool ConcludesConversation = false
);
