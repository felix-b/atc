using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging.Tests
{
    [TestFixture]
    public class BinaryLogStreamTests
    {
        private LogWriter _logWriter = LogWriter.Noop;
        private MemoryStream? _logOut = null;
        private BinaryLogStream? _logStream = null;
        private BinaryLogStreamWriter? _logStreamWriter = null;
        private DateTime _time = DateTime.UtcNow; 
        
        [SetUp]
        public void BeforeEach()
        {
            _time = new DateTime(2020, 5, 20, 10, 30, 0, DateTimeKind.Utc);
            _logOut = new MemoryStream();
            _logStream = new BinaryLogStream(_logOut);
            _logStreamWriter = (BinaryLogStreamWriter)_logStream.CreateWriter();
            _logWriter = new LogWriter(() => LogLevel.Debug, () => _time, () => _logStreamWriter);
        }

        [TearDown]
        public void AfterEach()
        {
            _logOut?.Dispose();
        }

        [Test]
        public void EmptyStream()
        {
            var root = ReadLog();
            root.Should().NotBeNull();
            root.Nodes.Should().BeEmpty();
        }

        [Test]
        public void SingleMessage()
        {
            _logWriter.Message("M1", LogLevel.Info);
            
            _logStreamWriter!.Flush();
            var root = ReadLog();

            root.Should().NotBeNull();
            root.Nodes.Count.Should().Be(1);

            root.Nodes[0].Depth.Should().Be(0);
            root.Nodes[0].Parent.Should().BeSameAs(root);
            root.Nodes[0].Level.Should().Be(LogLevel.Info);
            root.Nodes[0].MessageId.Should().Be("M1");
            root.Nodes[0].Values.Should().BeEmpty();
            root.Nodes[0].IsSpan.Should().BeFalse();
            root.Nodes[0].Duration.HasValue.Should().BeFalse();
        }

        [Test]
        public void SingleMessageWithValues()
        {
            _logWriter.Message("M1", LogLevel.Info, ("p1", "abc"), ("p2", 123));
            
            _logStreamWriter!.Flush();
            var root = ReadLog();

            root.Should().NotBeNull();
            root.Nodes.Count.Should().Be(1);

            root.Nodes[0].MessageId.Should().Be("M1");
            root.Nodes[0].Values.Count.Should().Be(2);
            
            root.Nodes[0].Values[0].Name.Should().Be("p1");
            root.Nodes[0].Values[0].Type.Should().Be(LogStreamOpCode.StringValue);
            root.Nodes[0].Values[0].Value.Should().Be("abc");

            root.Nodes[0].Values[1].Name.Should().Be("p2");
            root.Nodes[0].Values[1].Type.Should().Be(LogStreamOpCode.Int32Value);
            root.Nodes[0].Values[1].Value.Should().Be("123");
        }

        [Test]
        public void SingleSpan()
        {
            var span = _logWriter.Span("S1", LogLevel.Info);
            MoveTimeBy(seconds: 60);
            span.Dispose();
            
            var root = ReadLog();

            root.Should().NotBeNull();
            root.Nodes.Count.Should().Be(1);

            root.Nodes[0].Depth.Should().Be(0);
            root.Nodes[0].Parent.Should().BeSameAs(root);
            root.Nodes[0].Level.Should().Be(LogLevel.Info);
            root.Nodes[0].MessageId.Should().Be("S1");
            root.Nodes[0].Values.Should().BeEmpty();
            root.Nodes[0].IsSpan.Should().BeTrue();
            root.Nodes[0].Duration.HasValue.Should().BeTrue();
            root.Nodes[0].Duration!.Value.Should().Be(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void MultipleSequentialSpans()
        {
            var span1 = _logWriter.Span("S1", LogLevel.Info);
            MoveTimeBy(seconds: 60);
            span1.Dispose();
            MoveTimeBy(seconds: 30);
            var span2 = _logWriter.Span("S2", LogLevel.Info);
            MoveTimeBy(seconds: 30);
            span2.Dispose();
            
            var root = ReadLog();

            root.Should().NotBeNull();
            root.Nodes.Count.Should().Be(2);

            root.Nodes[0].Parent.Should().BeSameAs(root);
            root.Nodes[0].Depth.Should().Be(0);
            root.Nodes[0].MessageId.Should().Be("S1");
            root.Nodes[0].IsSpan.Should().BeTrue();

            root.Nodes[1].Parent.Should().BeSameAs(root);
            root.Nodes[1].Depth.Should().Be(0);
            root.Nodes[1].MessageId.Should().Be("S2");
            root.Nodes[1].IsSpan.Should().BeTrue();
        }

        private BinaryLogStreamReader.Node ReadLog()
        {
            _logOut!.Position = 0;
            var reader = new BinaryLogStreamReader(_logOut!);
            reader.ReadToEnd();
            return reader.RootNode;
        }

        private void MoveTimeBy(int seconds)
        {
            _time = _time.AddSeconds(seconds);
        }
    }
}