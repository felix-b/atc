using System.Threading.Tasks;
using AtcProto;
using Zero.Latency.Servers;

namespace Atc.Server
{
    public class WorldService
    {
        [PayloadCase(ClientToServer.PayloadOneofCase.connect)]
        public async ValueTask Connect(IConnectionContext<ServerToClient> connection, ClientToServer message)
        {
            if (message.connect.Token == "T12345")
            {
                await connection.SendMessage(new ServerToClient() {
                    reply_connect = new () {
                        ServerBanner = $"Hello new client, your connection id is {connection.Id}"
                    }
                });
            }
            else
            {
                await connection.CloseConnection();
            }
        }
    }
}