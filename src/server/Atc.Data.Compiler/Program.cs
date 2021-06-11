using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Atc.Data.Airports;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Atc.Data.Sources;
using Atc.Data.Sources.XP.Airports;
using Atc.Data.Traffic;
using Autofac;
using Zero.Doubt.Logging;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

[assembly:GenerateLogger(typeof(XPAptDatReader.ILogger))]

namespace Atc.Data.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Air Traffic & Control Cache Compiler (atcc)");
            ConsoleLog.Level = LogLevel.Debug;

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: atcc <full_path_apt_dat> <full_path_output_cache>");
            }
            
            BufferContextScope.UseStaticScope();            
            
            try
            {
                if (args[0] == "--info")
                {
                    PrintDataFileInfo();
                }
                else
                {
                    CompileDataFile();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            void CompileDataFile()
            {
                Console.WriteLine($"Input file:  {args[0]}");
                Console.WriteLine($"Output file: {args[1]}");

                var container = CompositionRoot();
                using var scope = AtcBufferContext.CreateEmpty(out var context);
                using var input = File.OpenRead(args[0]);
                using var output = File.Create(args[1]);

                var aptDatReader = container.Resolve<XPAptDatReader>();
                aptDatReader.ReadAptDat(input, OnQueryAirspace, onFilterAirport: null, onAirportLoaded: null);
                ((BufferContext) context).WriteTo(output);

                output.Flush();
            }

            void PrintDataFileInfo()
            {
                Console.WriteLine($"Retrieving info from data file: {args[1]}");
                using var file = File.OpenRead(args[1]);

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
        
        private static ZRef<ControlledAirspaceData> OnQueryAirspace(in AirportData.HeaderData header)
        {
            return AirspaceBuilder.AssembleSimpleAirspace(
                AirspaceType.ControlZone,
                AirspaceClass.B,
                name: header.Name,
                icaoCode: header.Icao,
                centerName: header.Icao,
                areaCode: header.Icao,
                centerPoint: header.Datum,
                radius: Distance.FromNauticalMiles(10),
                lowerLimit: null,
                upperLimit: Altitude.FromFeetMsl(18000)
            );
        }
        
        private static IContainer CompositionRoot()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(ConsoleLog.Writer).As<LogWriter>();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<XPAptDatReader.ILogger>()).AsImplementedInterfaces();
            builder.RegisterType<XPAptDatReader>().InstancePerDependency();

            return builder.Build();
        }
    }
}