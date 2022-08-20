using System.Reflection;
using Atc.Grains;

namespace Atc.Daemon;

public class AtcdSiloEnvironment : ISiloEnvironment
{
    private readonly string _devboxAssetRootPath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "..", "..", "..", "..", "..", "..", "assets");

    private readonly string _assetRootPath;
    private readonly IAtcdTelemetry _telemetry;

    public AtcdSiloEnvironment(IAtcdTelemetry telemetry, string? assetRootPath)
    {
        _telemetry = telemetry;
        _assetRootPath = assetRootPath ?? _devboxAssetRootPath;
    }
    
    public string GetAssetFilePath(string relativePath)
    {
        return Path.Combine(_assetRootPath, relativePath);
    }

    public DateTime UtcNow => DateTime.UtcNow;
}
