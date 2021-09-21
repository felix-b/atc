using System;
using System.Net;
using System.Threading;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public static class LogEngine
    {
        //TODO: verify AsyncLocal lifetime assumptions!
        private static readonly AsyncLocal<ILogStreamWriter?> _streamAsyncLocal = new();
        private static readonly Func<ILogStreamWriter> _getStream = () => {
            var stream = _streamAsyncLocal.Value;
            if (stream == null)
            {
                stream = _streamFactory.Invoke();
                _streamAsyncLocal.Value = stream;
            }
            return stream;
        };

        private static readonly Func<LogLevel> _getLevel = () => Level;
        private static readonly Func<DateTime> _getTime = () => DateTime.UtcNow;
        private static Func<ILogStreamWriter> _streamFactory = () => new NoopLogStreamWriter();

        public static void SetTargetToPipeline(params Func<ILogStreamWriter>[] factorySinks)
        {
            var factory = PipelineLogStreamWriter.CreateFactory(factorySinks)!;
            SetStreamFactory(factory);
        }

        public static void SetTargetToConsole()
        {
            SetStreamFactory(() => new ConsoleLogStreamWriter());
        }

        public static LogLevel Level { get; set; } = LogLevel.Info;

        public static LogWriter Writer { get; } = new LogWriter(_getLevel, _getTime, _getStream);

        private static void SetStreamFactory(Func<ILogStreamWriter> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            _streamFactory = factory;
        }
    }
}