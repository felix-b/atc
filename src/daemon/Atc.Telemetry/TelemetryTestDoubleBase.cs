using System.Collections.Concurrent;

namespace Atc.Telemetry;

public abstract class TelemetryTestDoubleBase
{
    private readonly ConcurrentQueue<string> _errors = new();
    private readonly ConcurrentQueue<string> _warnings = new();
    private readonly ConcurrentQueue<string> _allMessages = new();
    
    public IEnumerable<string> Errors => _errors;
    public IEnumerable<string> Warnings => _warnings;
    public IEnumerable<string> AllMessages => _allMessages;

    public void PrintAllToConsole()
    {
        Console.WriteLine("============ ALL TELEMETRY MESSAGES ============");
        foreach (var message in _allMessages)
        {
            Console.WriteLine(message);
        }
        Console.WriteLine("============ END OF TELEMETRY MESSAGES ============");
    }
    
    public void VerifyNoErrorsOr(Action<string> fail)
    {
        if (!_errors.IsEmpty)
        {
            fail($"Telemetry reported {_errors.Count} errors. 1st: {_errors.First()}");
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
        _errors.Enqueue(error);
        ReportMessage("Error", error);
    }

    protected void ReportWarning(string warning)
    {
        _warnings.Enqueue(warning);
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
        _allMessages.Enqueue($"{DateTime.Now:HH:mm:ss.fff} {level}|{message}");
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

        public void Fail(Exception error)
        {
            _failed = true;
            _owner.ReportError($"Span[{_message}] FAILED: {error.Message}");
        }
    }
}