using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Atc.Server
{
    public class EchoService : IWebSocketService
    {
        // private readonly BlockingCollection<Action> _requests = new(); 
        //
        // public void Dispose()
        // {
        //     throw new NotImplementedException();
        // }

        public async Task AcceptConnection(WebSocket scket, CancellationToken cancel)
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await scket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel);
            
            while (!receiveResult.CloseStatus.HasValue)
            {
                await scket.SendAsync(new ArraySegment<byte>(buffer, 0, receiveResult.Count), receiveResult.MessageType, receiveResult.EndOfMessage, cancel);
                receiveResult = await scket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel);
            }

            await scket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, cancel);
        }
    }
}
