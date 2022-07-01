using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Atc.Server;

public interface ISocketAcceptor : IAsyncDisposable
{
    Task AcceptSocket(HttpContext context, WebSocket socket, CancellationToken cancel);
}