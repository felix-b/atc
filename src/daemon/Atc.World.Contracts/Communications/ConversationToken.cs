namespace Atc.World.Contracts.Communications;

public record ConversationToken(
    ulong Id,
    AirGroundPriority Priority
    //, RadioStationType StationType 
);
