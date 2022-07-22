using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atc.Telemetry.CodePath;
using FluentAssertions;
using NUnit.Framework;
using static Atc.Telemetry.Tests.CodePath.CodePathTestUtility;

namespace Atc.Telemetry.Tests.CodePath;

[TestFixture]
public class CodePathStreamTests
{
        
    [SetUp]
    public void BeforeEach()
    {
    }

    [TearDown]
    public void AfterEach()
    {
    }

    [Test]
    public void EmptyStream()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");
        
        var root = ReadAll(environment);
        
        root.Should().NotBeNull();
        root.Nodes.Should().BeEmpty();
    }

    [Test]
    public void SingleMessage()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        writer.Message("M1", CodePathLogLevel.Info);

        var root = ReadAll(environment);

        root.Should().NotBeNull();
        root.Nodes.Count.Should().Be(1);

        root.Nodes[0].Depth.Should().Be(0);
        root.Nodes[0].Parent.Should().BeSameAs(root);
        root.Nodes[0].Time.Should().Be(environment.GetUtcNow());
        root.Nodes[0].Level.Should().Be(CodePathLogLevel.Info);
        root.Nodes[0].MessageId.Should().Be("M1");
        root.Nodes[0].Values.Should().BeEmpty();
        root.Nodes[0].IsSpan.Should().BeFalse();
        root.Nodes[0].EndTime.HasValue.Should().BeFalse();
        root.Nodes[0].Duration.HasValue.Should().BeFalse();
    }

    [Test]
    public void SingleMessageWithValues()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        writer.Message("M1", CodePathLogLevel.Info, ("p1", "abc"), ("p2", 123));
            
        var root = ReadAll(environment);

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
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        var span = writer.Span("S1", CodePathLogLevel.Debug);
        environment.MoveTimeBy(seconds: 60);
        span.Dispose();
            
        var root = ReadAll(environment);

        root.Should().NotBeNull();
        root.Nodes.Count.Should().Be(1);

        root.Nodes[0].Depth.Should().Be(0);
        root.Nodes[0].Parent.Should().BeSameAs(root);
        root.Nodes[0].Time.Should().Be(environment.StartUtc);
        root.Nodes[0].Level.Should().Be(CodePathLogLevel.Debug);
        root.Nodes[0].MessageId.Should().Be("S1");
        root.Nodes[0].Values.Should().BeEmpty();
        root.Nodes[0].IsSpan.Should().BeTrue();
        root.Nodes[0].EndTime.HasValue.Should().BeTrue();
        root.Nodes[0].EndTime!.Value.Should().Be(environment.StartUtc.AddMinutes(1));
        root.Nodes[0].Duration.HasValue.Should().BeTrue();
        root.Nodes[0].Duration!.Value.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Test]
    public void SingleSpanWithValues()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        var span = writer.Span(
            "S1", 
            CodePathLogLevel.Debug,
            ("str", "ABC"),
            ("num", 123)
        );
        
        environment.MoveTimeBy(seconds: 60);
        span.Dispose();
            
        var root = ReadAll(environment);

        root.Should().NotBeNull();
        root.Nodes.Count.Should().Be(1);

        root.Nodes[0].Depth.Should().Be(0);
        root.Nodes[0].Parent.Should().BeSameAs(root);
        root.Nodes[0].Time.Should().Be(environment.StartUtc);
        root.Nodes[0].Level.Should().Be(CodePathLogLevel.Debug);
        root.Nodes[0].MessageId.Should().Be("S1");
        root.Nodes[0].IsSpan.Should().BeTrue();
        root.Nodes[0].EndTime.HasValue.Should().BeTrue();
        root.Nodes[0].EndTime!.Value.Should().Be(environment.StartUtc.AddMinutes(1));
        root.Nodes[0].Duration.HasValue.Should().BeTrue();
        root.Nodes[0].Duration!.Value.Should().Be(TimeSpan.FromMinutes(1));

        root.Nodes[0].Values.Count.Should().Be(2);

        root.Nodes[0].Values[0].Name.Should().Be("str");
        root.Nodes[0].Values[0].Type.Should().Be(LogStreamOpCode.StringValue);
        root.Nodes[0].Values[0].Value.Should().Be("ABC");

        root.Nodes[0].Values[1].Name.Should().Be("num");
        root.Nodes[0].Values[1].Type.Should().Be(LogStreamOpCode.Int32Value);
        root.Nodes[0].Values[1].Value.Should().Be("123");
    }

    [Test]
    public void MultipleSequentialSpans()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        var span1 = writer.Span("S1", CodePathLogLevel.Debug);
        environment.MoveTimeBy(seconds: 60);
        span1.Dispose();
        environment.MoveTimeBy(seconds: 30);
        var span2 = writer.Span("S2", CodePathLogLevel.Debug);
        environment.MoveTimeBy(seconds: 30);
        span2.Dispose();
            
        var root = ReadAll(environment);

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

    [Test]
    public void CloseSpanWithException()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        var span1 = writer.Span("S1F", CodePathLogLevel.Debug);
        
        environment.MoveTimeBy(seconds: 20);
        span1.Fail(new Exception("test-exception"));

        environment.MoveTimeBy(seconds: 40);
        span1.Dispose();
        
        var root = ReadAll(environment);

        root.Nodes[0].IsSpan.Should().BeTrue();
        root.Nodes[0].Level.Should().Be(CodePathLogLevel.Debug);
        root.Nodes[0].MessageId.Should().Be("S1F");
        
        root.Nodes[0].EndTime.HasValue.Should().BeTrue();
        root.Nodes[0].EndTime!.Value.Should().Be(environment.StartUtc.AddMinutes(1));
        root.Nodes[0].Duration.HasValue.Should().BeTrue();
        root.Nodes[0].Duration!.Value.Should().Be(TimeSpan.FromMinutes(1));
        root.Nodes[0].Values.Count.Should().Be(1);
        root.Nodes[0].Values[0].Type.Should().Be(LogStreamOpCode.ExceptionValue);
        root.Nodes[0].Values[0].Name.Should().Be(string.Empty);
        root.Nodes[0].Values[0].Value.Should().Be("System.Exception: test-exception");
    }

    [Test]
    public void CloseSpanWithErrorCode()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");

        var span1 = writer.Span("S1F", CodePathLogLevel.Debug);
        
        environment.MoveTimeBy(seconds: 20);
        span1.Fail(errorCode: "TEST-ERR");

        environment.MoveTimeBy(seconds: 40);
        span1.Dispose();
        
        var root = ReadAll(environment);

        root.Nodes[0].IsSpan.Should().BeTrue();
        root.Nodes[0].Level.Should().Be(CodePathLogLevel.Debug);
        root.Nodes[0].MessageId.Should().Be("S1F");
        
        root.Nodes[0].EndTime.HasValue.Should().BeTrue();
        root.Nodes[0].EndTime!.Value.Should().Be(environment.StartUtc.AddMinutes(1));
        root.Nodes[0].Duration.HasValue.Should().BeTrue();
        root.Nodes[0].Duration!.Value.Should().Be(TimeSpan.FromMinutes(1));
        root.Nodes[0].Values.Count.Should().Be(1);
        root.Nodes[0].Values[0].Type.Should().Be(LogStreamOpCode.StringValue);
        root.Nodes[0].Values[0].Name.Should().Be("ErrorCode");
        root.Nodes[0].Values[0].Value.Should().Be("TEST-ERR");
    }

    [Test]
    public void NestedSpansAndMessages()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        var writer = new CodePathWriter(environment, "test");
        
        writer.Message("root-M1", CodePathLogLevel.Debug);
        using (var span1 = writer.Span("root-s1", CodePathLogLevel.Debug))
        {
            writer.Message("root-s1-M2", CodePathLogLevel.Debug);
            using (var span12 = writer.Span("root-s1-s12", CodePathLogLevel.Debug))
            {
                writer.Message("root-s1-s12-M3", CodePathLogLevel.Debug);
                writer.Message("root-s1-s12-M4", CodePathLogLevel.Debug);
            }
            writer.Message("root-s1-M5", CodePathLogLevel.Debug);
        }
        writer.Message("root-M6", CodePathLogLevel.Debug);
        
        var root = ReadAll(environment);

        root.Nodes.Count.Should().Be(3);
        
        AssertNode(root.Nodes[0], isSpan: false, messageId: "root-M1", childNodeCount: 0);
        AssertNode(root.Nodes[1], isSpan: true, messageId: "root-s1", childNodeCount: 3);
        AssertNode(root.Nodes[1].Nodes[0], isSpan: false, messageId: "root-s1-M2", childNodeCount: 0);
        AssertNode(root.Nodes[1].Nodes[1], isSpan: true, messageId: "root-s1-s12", childNodeCount: 2);
        
        AssertNode(root.Nodes[1].Nodes[1].Nodes[0], isSpan: false, messageId: "root-s1-s12-M3", childNodeCount: 0);
        AssertNode(root.Nodes[1].Nodes[1].Nodes[1], isSpan: false, messageId: "root-s1-s12-M4", childNodeCount: 0);

        AssertNode(root.Nodes[1].Nodes[2], isSpan: false, messageId: "root-s1-M5", childNodeCount: 0);
        AssertNode(root.Nodes[2], isSpan: false, messageId: "root-M6", childNodeCount: 0);
    }

    [Test]
    public async Task NestedSpansAndMessagesWithAsyncTasks()
    {
        var environment = CodePathTestDoubles.CreateEnvironment();
        environment.UseRealUtcNow();
        
        var writer = new CodePathWriter(environment, "test");

        //TODO: separate test
        // var longTask = async () => {
        //     await Task.Delay(150);
        //     writer.Message("root-MLONG", CodePathLogLevel.Debug);
        // };
        // Task? longTaskResult = null;

        var task11 = async () => {
            await Task.Delay(10);
            using (var span11 = writer.Span("root-s1-s11", CodePathLogLevel.Debug))
            {
                writer.Message("root-s1-s11-M3", CodePathLogLevel.Debug);
                writer.Message("root-s1-s11-M4", CodePathLogLevel.Debug);
            }
        };
        var task12 = async () => {
            using (var span12 = writer.Span("root-s1-s12", CodePathLogLevel.Debug))
            {
                writer.Message("root-s1-s12-M5", CodePathLogLevel.Debug);
                await Task.Delay(15);
                writer.Message("root-s1-s12-M6", CodePathLogLevel.Debug);
            }
        };

        var task1 = async () => {
            using (var span1 = writer.Span("root-s1", CodePathLogLevel.Debug))
            {
                var taskM2 = async () => {
                    await Task.Delay(15);
                    writer.Message("root-s1-M2", CodePathLogLevel.Debug);
                };
                
                //longTaskResult = longTask();
                
                var taskM7 = async () => {
                    await Task.Yield();
                    writer.Message("root-s1-M7", CodePathLogLevel.Debug);
                };
                await Task.WhenAll(task11(), task12(), taskM2(), taskM7());
            }
        };

        writer.Message("root-M1", CodePathLogLevel.Debug);
        await task1();
        writer.Message("root-M8", CodePathLogLevel.Debug);
        //await longTaskResult!;
        
        var root = ReadAll(environment);
        PrintNodeTree(root);
        
        root.Nodes.Count.Should().Be(3);
        
        AssertNode(root.Nodes[0], isSpan: false, messageId: "root-M1", childNodeCount: 0);
        AssertNode(root.Nodes[1], isSpan: true, messageId: "root-s1", childNodeCount: 4);
        
        AssertChildNode(root.Nodes[1], messageId: "root-s1-M2", isSpan: false, childNodeCount: 0);

        var nodeSpan11 = AssertChildNode(root.Nodes[1], messageId: "root-s1-s11", isSpan: true, childNodeCount: 2);
        AssertNode(nodeSpan11.Nodes[0], isSpan: false, messageId: "root-s1-s11-M3", childNodeCount: 0);
        AssertNode(nodeSpan11.Nodes[1], isSpan: false, messageId: "root-s1-s11-M4", childNodeCount: 0);
        
        var nodeSpan12 = AssertChildNode(root.Nodes[1], messageId: "root-s1-s12", isSpan: true, childNodeCount: 2);
        AssertNode(nodeSpan12.Nodes[0], isSpan: false, messageId: "root-s1-s12-M5", childNodeCount: 0);
        AssertNode(nodeSpan12.Nodes[1], isSpan: false, messageId: "root-s1-s12-M6", childNodeCount: 0);
        
        AssertChildNode(root.Nodes[1], messageId: "root-s1-M7", isSpan: false, childNodeCount: 0);
        
        AssertNode(root.Nodes[2], isSpan: false, messageId: "root-M8", childNodeCount: 0);
    }

}