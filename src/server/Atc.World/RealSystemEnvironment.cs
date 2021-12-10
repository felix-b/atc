using System;
using System.IO;
using System.Reflection;
using Atc.World.Abstractions;

namespace Atc.World
{
    public class RealSystemEnvironment : ISystemEnvironment
    {
        private static readonly string _executableFolderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        
        public string GetInstallRelativePath(string relativePath)
        {
            return Path.Combine(_executableFolderPath, relativePath);            
        }

        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}