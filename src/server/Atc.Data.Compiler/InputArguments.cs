using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Atc.Data.Compiler
{
    public class InputArguments
    {
        public string AtcFolderPath { get; private set; } = string.Empty;
        public string XPFolderPath { get; private set; } = string.Empty;
        public TaskType? Task { get; private set; } = null;
        public bool IsValid { get; private set; }

        public InputArguments(string[] args)
        {
            var argsQueue = new Queue<string>(args);
            var argHandlers = BuildArgumentHandlers(argsQueue);

            while (argsQueue.Count > 0)
            {
                var nextArg = argsQueue.Dequeue();
                if (!argHandlers.TryGetValue(nextArg, out var nextHandler) || !nextHandler())
                {
                    IsValid = false;
                    return;
                }
            }
            
            IsValid = true;
        }

        public bool Validate(bool xpFolderPathRequired)
        {
            if (string.IsNullOrEmpty(AtcFolderPath))
            {
                AtcFolderPath = 
                    Environment.GetEnvironmentVariable("ATC_DIR") 
                    ?? GetExecutableFolder();
            }
            
            if (string.IsNullOrEmpty(XPFolderPath))
            {
                XPFolderPath = Environment.GetEnvironmentVariable("XP_DIR") ?? string.Empty;
            }

            if (Task == null || Task == TaskType.None)
            {
                Task = TaskType.Compile;
            }
            
            if (string.IsNullOrEmpty(AtcFolderPath))
            {
                Console.WriteLine($"Error: ATC folder not set. Use --atc argument or ATC_DIR variable");
                IsValid = false;
            }

            if (!Directory.Exists(AtcFolderPath))
            {
                Console.WriteLine($"Error: ATC folder does not exist: {AtcFolderPath}");
                IsValid = false;
            }

            if (xpFolderPathRequired)
            {
                if (string.IsNullOrEmpty(XPFolderPath))
                {
                    Console.WriteLine($"Error: XP folder not set. Use --xp argument or XP_DIR variable");
                    IsValid = false;
                }

                if (!Directory.Exists(XPFolderPath))
                {
                    Console.WriteLine($"Error: XP folder not found: {XPFolderPath}");
                    IsValid = false;
                }
            }

            return IsValid;
        }

        public string DataCacheFilePath
        {
            get
            {
                return Path.Combine(
                    AtcFolderPath,
                    "cache",
                    "atc.cache"
                );
            }
        }

        private Dictionary<string, Func<bool>> BuildArgumentHandlers(Queue<string> argsQueue)
        {
            return new Dictionary<string, Func<bool>> {
                ["--xp"] = () => {
                    if (argsQueue.Count > 0)
                    {
                        XPFolderPath = argsQueue.Dequeue();
                        return true;
                    }
                    return false;
                },
                ["--atc"] = () => {
                    if (argsQueue.Count > 0)
                    {
                        AtcFolderPath = argsQueue.Dequeue();
                        return true;
                    }
                    return false;
                },
                ["build"] = () => {
                    Task = TaskType.Compile;
                    return true;
                },
                ["examine"] = () => {
                    Task = TaskType.Examine;
                    return true;
                },
            };
        }
        
        public static void PrintInstructions()
        {
            Console.WriteLine("Usage: atcc <command> [<option> [<option> ...]]");
            Console.WriteLine("Commands:");
            Console.WriteLine("  build             - compile cache file based on data sources");
            Console.WriteLine("  examine           - examine cache file and print detailed info");
            Console.WriteLine("Options:");
            Console.WriteLine("  --xp folder_path  - X-Plane folder where xplane.exe is located.");
            Console.WriteLine("                      alternatively, can be specified through XP_DIR env. variable");
            Console.WriteLine("                      only relevant for 'build' command");
            Console.WriteLine("  --atc folder_path - override folder where AT&C data is located");
            Console.WriteLine("                      default: the folder where atcc.exe is located");
            Console.WriteLine("                      alternatively, can be overridden through ATC_DIR env. variable");
        }

        private static string GetExecutableFolder()
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            return executablePath != null
                ? Path.GetDirectoryName(executablePath)!
                : string.Empty;
        }

        public enum TaskType
        {
            None = 0,
            Compile = 1,
            Examine = 2
        }
    }
}