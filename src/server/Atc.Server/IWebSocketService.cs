using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Atc.Server
{
    public interface IWebSocketService
    {
        Task AcceptConnection(WebSocket socket, CancellationToken cancel);
    }
}
