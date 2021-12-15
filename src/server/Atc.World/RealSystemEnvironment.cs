﻿using System;
using System.IO;
using System.Reflection;
using Atc.World.Abstractions;

namespace Atc.World
{
    public class RealSystemEnvironment : ISystemEnvironment
    {
        private static readonly string _executableFolderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        private readonly Random _random = new Random((int)(DateTime.Now.Ticks & 0xFFFFFFF));
        
        public string GetInstallRelativePath(string relativePath)
        {
            return Path.Combine(_executableFolderPath, relativePath);            
        }

        public int Random(int min, int max)
        {
            return _random.Next(min, max);
        }

        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}