using Atc.Server;
using AtcdProto;

namespace Atc.Daemon;

public class AtcdService
{
    [PayloadCase(AtcdClientToServer.PayloadOneofCase.connect_request)]
    public void Connect(
        IDeferredConnectionContext<AtcdServerToClient> connection, 
        AtcdClientToServer envelope)
    {
        var replyEnvelope = new AtcdServerToClient() {
            connect_reply = new AtcdServerToClient.ConnectReply() {
                Success = true,
                Error = string.Empty
            }
        };

        connection.FireMessage(replyEnvelope);
    }
    
    [PayloadCase(AtcdClientToServer.PayloadOneofCase.disconnect_request)]
    public void Disconnect(
        IDeferredConnectionContext<AtcdServerToClient> connection, 
        AtcdClientToServer envelope)
    {
        connection.DisposeObserver(connection.Id.ToString());
        connection.RequestClose();
    }
}
