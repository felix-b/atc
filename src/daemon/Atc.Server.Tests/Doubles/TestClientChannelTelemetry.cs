using System.Net.WebSockets;
using Atc.Telemetry;

namespace Atc.Server.Tests.Doubles;

public class TestClientChannelTelemetry : TelemetryTestDoubleBase, IClientChannelTelemetry
{
    void IClientChannelTelemetry.DebugClientSocketConnecting(string url)
    {
        ReportDebug($"ClientSocketConnecting(url={url})");
    }

    void IClientChannelTelemetry.InfoClientSocketConnected(string url)
    {
        ReportInfo($"ClientSocketConnected(url={url})");
    }

    void IClientChannelTelemetry.DebugClientReceiveLoopWaitingForConnect()
    {
        ReportDebug($"ClientReceiveLoopWaitingForConnect");
    }

    void IClientChannelTelemetry.DebugClientReceiveLoopStarting()
    {
        ReportDebug($"ClientReceiveLoopStarting");
    }

    void IClientChannelTelemetry.DebugClientReceivedFrame(int bytesReceived, WebSocketMessageType messageType)
    {
        ReportDebug($"ClientReceivedFrame(bytesReceived={bytesReceived},messageType={messageType})");
    }

    void IClientChannelTelemetry.DebugClientReceivedMessage(int sizeBytes)
    {
        ReportDebug($"ClientReceivedMessage(sizeBytes={sizeBytes})");
    }

    void IClientChannelTelemetry.DebugClientReceiveLoopCancelled()
    {
        ReportDebug($"ClientReceiveLoopCancelled");
    }

    void IClientChannelTelemetry.ErrorClientSocketError(WebSocketError code, WebSocketException exception)
    {
        ReportError($"ClientSocketError(code={code},exception={exception.GetType().Name}:{exception.Message})");
    }

    void IClientChannelTelemetry.DebugClientReceiveLoopExited(SocketReceiveStatus status)
    {
        ReportDebug($"ClientReceiveLoopExited(status={status})");
    }

    void IClientChannelTelemetry.DebugClientSocketInitiatingCloseHandshake()
    {
        ReportDebug($"ClientSocketInitiatingCloseHandshake");
    }

    void IClientChannelTelemetry.DebugClientJoiningReceiveLoop()
    {
        ReportDebug($"ClientJoiningReceiveLoop");
    }

    ITraceSpan IClientChannelTelemetry.SpanClientSocketDisposing()
    {
        return new TestSpan(this, "ClientSocketDisposing");
    }

    void IClientChannelTelemetry.InfoClientSocketDisconnected()
    {
        ReportInfo($"ClientSocketDisconnected");
    }

    void IClientChannelTelemetry.DebugClientReplyingSocketCloseHandshake()
    {
        ReportDebug($"ClientReplyingSocketCloseHandshake");
    }
}
