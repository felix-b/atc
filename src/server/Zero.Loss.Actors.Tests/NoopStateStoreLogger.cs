using System;
using Zero.Doubt.Logging;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors.Tests
{
    public class NoopStateStoreLogger : StateStore.ILogger
    {
        public LogWriter.LogSpan Dispatch(ulong sequenceNo, string targetId, string eventType, string? eventData)
        {
            return LogWriter.LogSpan.Noop();
        }

        public void ListenerFailed(ulong sequenceNo, int listenerId, string eventType, Exception error)
        {
        }

        public void ResetNextSequenceNo(ulong value)
        {
        }
    }
}