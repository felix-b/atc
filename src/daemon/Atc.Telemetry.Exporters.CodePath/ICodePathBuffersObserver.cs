namespace Atc.Telemetry.Exporters.CodePath;

public interface ICodePathBuffersObserver
{
    void SendBuffer(MemoryStream buffer);
}
