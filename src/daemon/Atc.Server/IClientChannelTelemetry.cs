using System.Net.WebSockets;
using Atc.Telemetry;

namespace Atc.Server;

public interface IClientChannelTelemetry : ITelemetry
{
    void DebugClientSocketConnecting(string url);
    void InfoClientSocketConnected(string url);
    void DebugClientReceiveLoopWaitingForConnect();
    void DebugClientReceiveLoopStarting();
    void DebugClientReceivedFrame(int bytesReceived, WebSocketMessageType messageType);
    void DebugClientReceivedMessage(int sizeBytes);
    void DebugClientReceiveLoopCancelled();
    void ErrorClientSocketError(WebSocketError code, WebSocketException exception);
    void DebugClientReceiveLoopExited(SocketReceiveStatus status);
    void DebugClientSocketInitiatingCloseHandshake();
    void DebugClientJoiningReceiveLoop();
    ITraceSpan SpanClientSocketDisposing();
    void InfoClientSocketDisconnected();
    void DebugClientReplyingSocketCloseHandshake();
}

