#if false

using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging.Tests
{
    public class Tests
    {
        [Test]
        public void CanLogMessage()
        {
            Log.Debug("test-message");

            var entries = Log.Engine.CurrentStream.GetWrittenEntries().ToArray();
            entries.Length.Should().Be(1);
            entries[0].OpCode.Should().Be(LogStreamOpCode.MessageDebugEmpty);
            entries[0].Key.Should().Be("test-message");
        }

        [Test]
        public void CanLogMessageWithKeyValuePairs()
        {
            Log.Debug("test-message", ("num", 123), ("str", "xyz"));

            var entries = Log.Engine.CurrentStream.GetWrittenEntries().ToArray();
            entries.Length.Should().Be(4);
            
            entries[0].OpCode.Should().Be(LogStreamOpCode.MessageDebugOpen);
            entries[0].Key.Should().Be("test-message");
            
            entries[1].OpCode.Should().Be(LogStreamOpCode.Int32Value);
            entries[1].Key.Should().Be("num");
            entries[1].Value.Should().Be(123);

            entries[2].OpCode.Should().Be(LogStreamOpCode.StringValue);
            entries[2].Key.Should().Be("str");
            entries[2].Value.Should().Be("abc");

            entries[3].OpCode.Should().Be(LogStreamOpCode.CloseMessage);
        }
        
        // public class TestLogger
        // {
        //     void ASimpleMessage()
        //     {
        //         Log.Debug(nameof(ASimpleMessage));
        //     }
        // }
    }
}
#endif