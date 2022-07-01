namespace Atc.Server;

public interface IReadOnlyConnectionContext
{
    long Id { get; }
    bool IsActive { get; }
    CancellationToken Cancellation { get; }
        
    //TODO: add more info about the client/connection?
}

public interface IConnectionContext : IReadOnlyConnectionContext
{
    ValueTask SendMessage(object outgoingMessageEnvelope);
    void RegisterObserver(IObserverSubscription observer, string? registrationKey);
    ValueTask DisposeObserver(string registrationKey);
    ValueTask CloseConnection();
    SessionItems Session { get; }
}