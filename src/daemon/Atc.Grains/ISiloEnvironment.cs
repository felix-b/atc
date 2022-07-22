namespace Atc.Grains;

public interface ISiloEnvironment
{
    string GetAssetFilePath(string relativePath);
    DateTime UtcNow { get; }
}
