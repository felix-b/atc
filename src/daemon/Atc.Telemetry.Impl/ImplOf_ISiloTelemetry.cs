using Atc.Grains;
using Atc.Telemetry.CodePath;

// ReSharper disable InconsistentNaming

namespace Atc.Telemetry.Impl;

public static class ImplOf_ISiloTelemetry
{
    // public class Noop : ISiloTelemetry
    // {
    // }
    
    public class CodePath : ISiloTelemetry
    {
        private static readonly string __s_dispatchEvent = "DispatchEvent";
        private static readonly string __s_grainId = "grainId";
        private static readonly string __s_eventType = "eventType";
        private static readonly string __s_executeReadyWorkItems = "ExecuteReadyWorkItems";
        private static readonly string __s_workItem = "workItem";
        private static readonly string __s_timedOut = "timedOut";

        private readonly ICodePathEnvironment _environment;
        private readonly CodePathWriter _writer;

        public CodePath(ICodePathEnvironment environment)
        {
            _environment = environment;
            _writer = new(_environment, "Silo");
        }

        public void DebugDispatchEvent(string grainId, IGrainEvent @event)
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteBeginMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_dispatchEvent, LogLevel.Debug);
            buffer.WriteValue(__s_grainId, grainId);
            buffer.WriteValue(__s_eventType, @event.GetType().Name);
            buffer.WriteEndMessage();
            buffer.Flush();
        }

        public ITraceSpan SpanExecuteReadyWorkItems()
        {
            _writer.SpawnNewSpan(out var spanId, out var parentSpanId);

            var buffer = _environment.NewBuffer();
            buffer.WriteOpenSpan(spanId, parentSpanId, _environment.GetUtcNow(), __s_executeReadyWorkItems, LogLevel.Verbose);
            buffer.Flush();
        
            return new CodePathWriter.TraceSpan(_writer, spanId, parentSpanId);
        }

        public ITraceSpan SpanExecuteWorkItem(string grainId, IGrainWorkItem workItem, bool timedOut)
        {
            _writer.SpawnNewSpan(out var spanId, out var parentSpanId);

            var buffer = _environment.NewBuffer();
            buffer.WriteBeginOpenSpan(spanId, parentSpanId, _environment.GetUtcNow(), __s_executeReadyWorkItems, LogLevel.Verbose);
            buffer.WriteValue(__s_grainId, grainId);
            buffer.WriteValue(__s_workItem, workItem.GetType().Name);
            buffer.WriteValue(__s_timedOut, timedOut);
            buffer.WriteEndOpenSpan();
            buffer.Flush();
        
            return new CodePathWriter.TraceSpan(_writer, spanId, parentSpanId);
        }
    }

}
