namespace Atc.Server;

public interface IServiceHostContext
{
    ValueTask RemoveClosedConnection(Connection connection);
        
    IMessageSerializer Serializer { get; }
        
    IOperationDispatcher Dispatcher { get; }
        
    IEndpointTelemetry Telemetry { get; }
}