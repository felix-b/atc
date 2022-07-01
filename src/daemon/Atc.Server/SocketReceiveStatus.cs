using System.Net.WebSockets;

namespace Atc.Server;

public enum SocketReceiveStatus
{
    Unknown,
    MessageReceived,
    ProtocolError,
    SocketClosing,
    ConnectionCanceled
}
