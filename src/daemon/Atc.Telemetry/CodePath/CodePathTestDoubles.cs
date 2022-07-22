using System.Threading.Channels;

namespace Atc.Telemetry.CodePath;

public static class CodePathTestDoubles
{
    public static readonly DateTime StartUtcValue = 
        new DateTime(2022, 10, 10, 8, 30, 0, DateTimeKind.Utc);

    public static TestEnvironment CreateEnvironment(CodePathLogLevel logLevel = CodePathLogLevel.Debug)
    {
        var exporter = new TestExporter();
        return new TestEnvironment(logLevel, exporter);
    }
    
    public class TestEnvironment : CodePathEnvironment
    {
        public TestEnvironment(CodePathLogLevel logLevel, TestExporter exporter) 
            : base(logLevel, exporter)
        {
            this.Exporter = exporter;
            exporter.InjectEnvironment(this);
        }

        public override DateTime GetUtcNow()
        {
            return PresetUtcNow ?? DateTime.UtcNow;
        }

        public void MoveTimeBy(int seconds)
        {
            PresetUtcNow = (PresetUtcNow ?? StartUtc).AddSeconds(seconds);
        }

        public void UseRealUtcNow()
        {
            PresetUtcNow = null;
        }

        public DateTime StartUtc => StartUtcValue;
        public DateTime? PresetUtcNow { get; set; } = StartUtcValue;
        public TestExporter Exporter { get; }
    }

    public class TestExporter : ICodePathExporter
    {
        public TestExporter()
        {
            Buffers = Channel.CreateBounded<MemoryStream>(capacity: 1000);            
        }

        public void InjectEnvironment(ICodePathEnvironment environment)
        {
            //noop
        }

        public void PushBuffer(MemoryStream buffer)
        {
            if (!Buffers.Writer.TryWrite(buffer))
            {
                throw new InvalidOperationException("CodePath test exporter failed: the queue is full");
            }
        }

        public Channel<MemoryStream> Buffers { get; }
    }
}
