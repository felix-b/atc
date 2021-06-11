using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Atc.Data.Sources;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Compiler
{
    public class ExamineCacheTask : ICompilerTask
    {
        public bool ValidateArguments(InputArguments args)
        {
            return args.Validate(xpFolderPathRequired: false);
        }

        public void Execute(InputArguments args)
        {
            Console.WriteLine($"Examining cache file: {args.DataCacheFilePath}");
            using var file = File.OpenRead(args.DataCacheFilePath);

            var info = BufferContext.ReadInfoFrom(file);
            file.Position = 0;

            var clock = Stopwatch.StartNew();
            using var scope = AtcBufferContext.LoadFrom(file, out var context);
            clock.Stop();
            
            Console.WriteLine($"File Size: {file.Length:#,##0} bytes ({(float)file.Length / 1024 / 1024:0.00} MB)");
            Console.WriteLine($"Load Time: {clock.ElapsedMilliseconds:#,##0} ms");
            
            var sortedBuffers = info.Buffers.OrderByDescending(b => b.BufferSizeBytes);

            foreach (var bufferInfo in sortedBuffers)
            {
                string typeName = GetTypeFriendlyName(bufferInfo.RecordType);
                var buffer = context.GetBuffer(bufferInfo.RecordType);

                Console.WriteLine();
                Console.WriteLine($"=== Buffer:       {SplitStringForDisplay(typeName, maxLength: 60, indentSize: 18)}");
                Console.WriteLine($"    Size:         {bufferInfo.BytesToSkipToEndOfBuffer:#,##0}");
                Console.WriteLine($"    Record Size:  {bufferInfo.RecordSizeBytes}");                    
                Console.WriteLine($"    Record Count: {bufferInfo.RecordCount:#,##0}");                    
                Console.WriteLine($"    Var Size:     {(bufferInfo.IsVariableSize ? "Yes" : "No")}");
                Console.WriteLine($"    -- specialized info --");

                var specialInfo = buffer.GetSpecializedInfo().ToArray();
                if (specialInfo.Length > 0)
                {
                    foreach (var pair in specialInfo)
                    {
                        Console.WriteLine($"    {pair.Label,-12}: {pair.Value}");
                    }
                }
                else
                {
                    Console.WriteLine($"    (none available)");
                }
            }

            Console.WriteLine($"--- Total {info.Buffers.Count} buffer(s) ---");                    
        }

        private static string GetTypeFriendlyName(Type type)
        {
            var result = new StringBuilder();
            AppendTypeFriendlyName(type, result);
            return result.ToString();
        }

        private static void AppendTypeFriendlyName(Type type, StringBuilder destination)
        {
            var simpleName = type.Name ?? "n/a";
            var backApostropheIndex = simpleName.IndexOf('`');

            destination.Append(
                backApostropheIndex < 0
                    ? simpleName
                    : simpleName.Substring(0, backApostropheIndex));
            
            if (!type.IsGenericType)
            {
                return;
            }

            var args = type.GetGenericArguments();
            destination.Append('<');

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    destination.Append(',');
                }
                AppendTypeFriendlyName(args[i], destination);
            }
            
            destination.Append('>');
        }

        private static string SplitStringForDisplay(string s, int maxLength, int indentSize)
        {
            var result = new StringBuilder();
            var indentString = new string(' ', indentSize);

            for (var remainder = s; remainder.Length > 0 ; )
            {
                if (result.Length > 0)
                {
                    result.Append(Environment.NewLine);
                    result.Append(indentString);
                }

                if (remainder.Length <= maxLength)
                {
                    result.Append(remainder);
                    remainder = string.Empty;
                }
                else
                {
                    result.Append(remainder.Substring(0, maxLength));
                    remainder = remainder.Substring(maxLength);
                }
            }

            return result.ToString();
        }
    }
}