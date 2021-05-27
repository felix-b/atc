using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Zero.Latency.Servers
{
    public interface ISocketAcceptor : IAsyncDisposable
    {
        Task AcceptSocket(HttpContext context, WebSocket socket, CancellationToken cancel);
    }
}
