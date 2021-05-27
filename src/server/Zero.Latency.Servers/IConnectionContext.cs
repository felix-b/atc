using System.Threading;
using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IConnectionContext<TOutgoingMessage>
        where TOutgoingMessage : class
    {
        ValueTask SendMessage(TOutgoingMessage message);
        void RegisterObserver(IObserverSubscription observer);
        ValueTask CloseConnection();
        long Id { get; }
        bool IsActive { get; }
        CancellationToken Cancellation { get; }
        
        //TODO: add more info about the client/connection?
    }
}