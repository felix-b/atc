using Atc.Data.Compiler;
using Atc.Data.Sources.Embedded;
using Zero.Doubt.Logging;
using Atc.Data.Sources.XP.Airports;

[assembly:GenerateLogger(typeof(ICompilerLogger))]
[assembly:GenerateLogger(typeof(IEmbeddedDataSourcesLogger))]
[assembly:GenerateLogger(typeof(XPAptDatReader.ILogger))]

