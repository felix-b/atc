namespace Atc.World.Contracts.Communications;

public record ConversationToken(
    ulong Id
) {
    public override string ToString()
    {
        return $"#{Id}";
    }
}
