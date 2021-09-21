using System;
using Atc.Data.Sources.Embedded;
using Autofac;
using Zero.Doubt.Logging;
using Zero.Serialization.Buffers;
using Atc.Data.Sources.XP.Airports;

namespace Atc.Data.Compiler
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("atcc 0.1.0 - Air Traffic & Control Cache Compiler");

            var arguments = ParseArguments(args);
            if (arguments == null)
            {
                return 1;
            }
            
            try
            {
                LogEngine.Level = LogLevel.Debug;
                BufferContextScope.UseStaticScope();            

                var container = CompositionRoot();
                var task = CreateTask(arguments, container);

                if (!task.ValidateArguments(arguments))
                {
                    InputArguments.PrintInstructions();
                    return 1;
                }

                task.Execute(arguments);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 100;
            }
        }

        private static InputArguments? ParseArguments(string[] args)
        {
            var arguments = new InputArguments(args);
            if (arguments.IsValid)
            {
                return arguments;
            }

            InputArguments.PrintInstructions();
            return null;
        }
        
        private static IContainer CompositionRoot()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(LogEngine.Writer).As<LogWriter>();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<ICompilerLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<IEmbeddedDataSourcesLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<XPAptDatReader.ILogger>()).AsImplementedInterfaces();
            builder.RegisterType<XPAptDatReader>().InstancePerDependency();

            builder.RegisterType<CompileCacheTask>().InstancePerDependency();
            builder.RegisterType<ExamineCacheTask>().InstancePerDependency();

            builder.RegisterType<RegionDatReader>().InstancePerDependency();
            builder.RegisterType<AirlineDatReader>().InstancePerDependency();
            builder.RegisterType<TypeJsonReader>().InstancePerDependency();
            builder.RegisterType<RouteDatReader>().InstancePerDependency();

            return builder.Build();
        }

        private static ICompilerTask CreateTask(InputArguments args, IContainer container)
        {
            return args.Task switch {
                InputArguments.TaskType.Compile =>
                    container.Resolve<CompileCacheTask>(),  
                InputArguments.TaskType.Examine =>
                    container.Resolve<ExamineCacheTask>(),
                _ => throw new Exception("Unknown task")
            };
        }
    }
}