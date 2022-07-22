using System.Collections.Concurrent;

namespace Atc.Telemetry;

public abstract class TelemetryTestDoubleBase
{
    private readonly ConcurrentQueue<LogEntry> _errors = new();
    private readonly ConcurrentQueue<LogEntry> _warnings = new();
    private readonly ConcurrentQueue<LogEntry> _allMessages = new();
    
    public IEnumerable<string> Errors => _errors.Select(entry => entry.Message);
    public IEnumerable<string> Warnings => _warnings.Select(entry => entry.Message);
    public IEnumerable<string> AllMessages => _allMessages.Select(entry => entry.Message);

    public void PrintAllToConsole()
    {
        Console.WriteLine("============ ALL TELEMETRY MESSAGES ============");
        foreach (var entry in _allMessages)
        {
            Console.WriteLine($"{entry.Time:HH:mm:ss.fff} {entry.Message}");
        }
        Console.WriteLine("============ END OF TELEMETRY MESSAGES ============");
    }
    
    public void VerifyNoErrorsOr(Action<string> fail)
    {
        if (!_errors.IsEmpty)
        {
            var errorMessages = string.Join('\n', _errors.Take(3).Select((e, index) => $"\n{index}: {e.ToString()}"));
            fail($"Telemetry reported {_errors.Count} errors: {errorMessages}");
        }
    }

    public void VerifyNoErrorsNoWarningsOr(Action<string> fail)
    {
        VerifyNoErrorsOr(fail);
        
        if (!_warnings.IsEmpty)
        {
            fail($"Telemetry reported {_warnings.Count} warnings. 1st: {_warnings.First()}");
        }
    }

    protected void ReportError(string error)
    {
        _errors.Enqueue(new LogEntry(DateTime.Now, error));
        ReportMessage("Error", error);
    }

    protected void ReportWarning(string warning)
    {
        _warnings.Enqueue(new LogEntry(DateTime.Now, warning));
        ReportMessage("Warning", warning);
    }

    protected void ReportInfo(string message)
    {
        ReportMessage("Info", message);
    }
    
    protected void ReportVerbose(string message)
    {
        ReportMessage("Verbose", message);
    }

    protected void ReportDebug(string message)
    {
        ReportMessage("Debug", message);
    }

    protected void ReportSpan(string message)
    {
        ReportMessage("Trace|Begin", message);
    }

    protected void ReportEndSpan(string message, bool success)
    {
        ReportMessage($"Trace|End|{(success ? "OK" : "FAIL")}", message);
    }

    private void ReportMessage(string level, string message)
    {
        _allMessages.Enqueue(new LogEntry(DateTime.Now, $"{level}|{message}"));
    }

    protected class TestSpan : ITraceSpan
    {
        private readonly TelemetryTestDoubleBase _owner;
        private readonly string _message;
        private bool _failed = false;

        public TestSpan(TelemetryTestDoubleBase owner, string message)
        {
            _owner = owner;
            _message = message;
            _owner.ReportSpan(message);
        }

        public void Dispose()
        {
            _owner.ReportEndSpan(_message, success: !_failed);
        }

        public void Fail(Exception exception)
        {
            _failed = true;
            _owner.ReportError($"Span[{_message}] FAILED, EXCEPTION: {exception.Message}");
        }

        public void Fail(string errorCode)
        {
            _failed = true;
            _owner.ReportError($"Span[{_message}] FAILED, ERROR {errorCode}");
        }
    }

    private readonly struct LogEntry
    {
        public readonly DateTime Time;
        public readonly string Message;

        public LogEntry(DateTime time, string message)
        {
            Time = time;
            Message = message;
        }

        public override string ToString()
        {
            return $"{Time:HH:mm:ss.fff} {Message}";
        }
    }
}
