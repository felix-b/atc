namespace Atc.Server;

public interface IDeferredConnectionContext<TEnvelopeOut> : IReadOnlyConnectionContext
    where TEnvelopeOut : class
{
    /// <summary>
    /// When an observer needs to push messages to a client,
    /// (or in any case when the connection object received by an operation method is cached and used
    /// after returning from the operation method),  
    /// it must use a new instance of the context, obtained by calling this method.   
    /// </summary>
    /// <returns>
    /// A new copy of the context that can be used to push new messages to client. 
    /// </returns>
    IDeferredConnectionContext<TEnvelopeOut> CopyForPush();
    
    void FireMessage(TEnvelopeOut outgoingMessageEnvelope);
    
    void RegisterObserver(IObserverSubscription observer, string? registrationKey = null);
    
    void DisposeObserver(string registrationKey);
    
    void RequestClose();
    
    void RequestFlush();
    
    SessionItems Session { get; }
}
