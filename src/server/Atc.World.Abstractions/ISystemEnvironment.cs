using System;
using System.IO;

namespace Atc.World.Abstractions
{
    public interface ISystemEnvironment
    {
        string GetInstallRelativePath(string relativePath);
        int Random(int min, int max);
        DateTime UtcNow();

        public string GetAssetFilePath(string pathRelativeToAssetsFolder)
        {
            return GetInstallRelativePath(Path.Combine("assets", pathRelativeToAssetsFolder));
        }
    }
}
