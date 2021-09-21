using System.Threading.Tasks;

namespace Zero.Latency.Servers
{
    public interface IServiceHostContext
    {
        ValueTask CloseConnection(Connection connection);
        
        IMessageSerializer Serializer { get; }
        
        IOperationDispatcher Dispatcher { get; }
        
        IEndpointLogger Logger { get; }
    }
}
