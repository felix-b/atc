using System;
using System.IO;

namespace Atc.World.Abstractions
{
    public interface ISystemEnvironment
    {
        string GetInstallRelativePath(string relativePath);
        DateTime UtcNow();

        public string GetAssetFilePath(string pathRelativeToAssetsFolder)
        {
            return GetInstallRelativePath(Path.Combine("assets", pathRelativeToAssetsFolder));
        }
    }
}
