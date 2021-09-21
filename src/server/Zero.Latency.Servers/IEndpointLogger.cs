using System;
using Zero.Doubt.Logging;

namespace Zero.Latency.Servers
{
    public interface IEndpointLogger
    {
        void RegisteringObserver(long connectionId, Type type, string? registrationKey);
        void DisposingObserver(long connectionId, string? registrationKey);
        LogWriter.LogSpan DoingSomething(long connectionId, string? registrationKey);
        Exception SomeError(long connectionId, string? registrationKey);
        void EndpointDisposingAsync(int step);
        void HostRunStarting();
        void HostRunFinished();
        void HostStoppingAsync(int step);
    }

    // internal class EndpointLogger : IEndpointLogger
    // {
    //     
    // }
}
