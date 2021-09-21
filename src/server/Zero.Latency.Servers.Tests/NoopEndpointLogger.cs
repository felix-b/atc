using System;
using Zero.Doubt.Logging;

namespace Zero.Latency.Servers.Tests
{
    public class NoopEndpointLogger : IEndpointLogger
    {
        public void RegisteringObserver(long connectionId, Type type, string? registrationKey)
        {
        }

        public void DisposingObserver(long connectionId, string? registrationKey)
        {
        }

        public LogWriter.LogSpan DoingSomething(long connectionId, string? registrationKey)
        {
            return LogWriter.LogSpan.Noop();
        }

        public Exception SomeError(long connectionId, string? registrationKey)
        {
            return new Exception(nameof(SomeError));
        }

        public void EndpointDisposingAsync(int step)
        {
        }

        public void HostRunStarting()
        {
        }

        public void HostRunFinished()
        {
        }

        public void HostStoppingAsync(int step)
        {
        }
    }
}