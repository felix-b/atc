namespace Atc.Grains;

public interface ISiloEnvironment
{
    string GetAssetFilePath(string relativePath); //TODO: move from here
    DateTime UtcNow { get; }
}
