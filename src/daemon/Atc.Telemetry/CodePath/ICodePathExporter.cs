namespace Atc.Telemetry.CodePath;

public interface ICodePathExporter
{
    void InjectEnvironment(ICodePathEnvironment environment);
    void PushBuffer(MemoryStream buffer);
}
