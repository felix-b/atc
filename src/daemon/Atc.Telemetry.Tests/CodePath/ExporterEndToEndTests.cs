using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atc.Grains;
using Atc.Server.TestDoubles;
using Atc.Telemetry.CodePath;
using Atc.Telemetry.Exporters.CodePath;
using AtcTelemetryCodepathProto;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
using NUnit.Framework;
using TestCodePathClient = Atc.Server.TestDoubles.TestServiceClient<
    AtcTelemetryCodepathProto.CodePathClientToServer, 
    AtcTelemetryCodepathProto.CodePathServerToClient
>;
using static Atc.Telemetry.Tests.CodePath.CodePathTestUtility;

namespace Atc.Telemetry.Tests.CodePath;

[TestFixture, Category("manual")]
public class ExporterEndToEndTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        TestServiceClientAssert.Fail = Assert.Fail;
        TestServiceClientAssert.NotNull = Assert.NotNull;
        TestServiceClientAssert.IsTrue = Assert.IsTrue;
    }

    [Test]
    public void CanStartAndStopExporter()
    {
        var exporter = new CodePathWebSocketExporter(listenPortNumber: 3003);
        exporter.Dispose();
    }

    [Test]
    public void CanPublishTelemetryWithoutConnectedClient()
    {
        var exporter = new CodePathWebSocketExporter(listenPortNumber: 3003);
        var environment = new CodePathEnvironment(CodePathLogLevel.Debug, exporter);

        var buffer = environment.NewBuffer();
        buffer.WriteMessage(parentSpanId: 0, environment.GetUtcNow(), "M1", CodePathLogLevel.Debug);
        buffer.Flush();
        
        Thread.Sleep(100);
        
        exporter.Dispose();
    }

    [Test]
    public async Task CanConnectAndDisconnectClient()
    {
        TestEndpointTelemetry serverTelemetry = new TestEndpointTelemetry();
        TestClientChannelTelemetry clientTelemetry;

        using var exporter = new CodePathWebSocketExporter(listenPortNumber: 3003, serverTelemetry);
        var environment = new CodePathEnvironment(CodePathLogLevel.Debug, exporter);

        await using (var client = CreateCodePathClient(out clientTelemetry))
        {
            await client.SendEnvelope(new CodePathClientToServer() {
                connect_request = new CodePathClientToServer.ConnectRequest()
            });

            await client.WaitForIncomingEnvelope(
                predicate: e => e.connect_reply != null, 
                millisecondsTimeout: 1000);
            // await client.WaitForIncomingEnvelopes(
            //     predicate: e => e.telemetry_buffer != null,
            //     count: 14,
            //     millisecondsTimeout: 1000);

            await client.SendEnvelope(new CodePathClientToServer {
                disconnect_request = new CodePathClientToServer.DisconnectRequest()
            });
        }

        exporter.Dispose();

        serverTelemetry.PrintAllToConsole();
        clientTelemetry.PrintAllToConsole();

        serverTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
        clientTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }
    
    [Test]
    public async Task CanConnectClientAndReceiveTelemetry()
    {
        TestEndpointTelemetry serverTelemetry = new TestEndpointTelemetry();
        TestClientChannelTelemetry clientTelemetry;

        using var exporter = new CodePathWebSocketExporter(listenPortNumber: 3003, serverTelemetry);
        var environment = new CodePathEnvironment(CodePathLogLevel.Debug, exporter);
        var client = CreateCodePathClient(out clientTelemetry);

        await using (client)
        {
            await client.SendEnvelope(new CodePathClientToServer() {
                connect_request = new CodePathClientToServer.ConnectRequest()
            });

            await client.WaitForIncomingEnvelope(
                predicate: e => e.connect_reply != null, 
                millisecondsTimeout: 1000);

            await PlayTelemetryExampleForUnitTest(environment);
            
            await client.WaitForIncomingEnvelopes(
                predicate: e => e.telemetry_buffer != null,
                count: 14,
                millisecondsTimeout: 1000);

            await client.SendEnvelope(new CodePathClientToServer {
                disconnect_request = new CodePathClientToServer.DisconnectRequest()
            });
        }

        exporter.Dispose();

        serverTelemetry.PrintAllToConsole();
        clientTelemetry.PrintAllToConsole();

        Console.WriteLine($"exporter.TotalBufferCount={exporter.TotalBufferCount}");
        Console.WriteLine($"exporter.Service.TotalObserveBuffersCount={exporter.Service.TotalObserveBuffersCount}");
        Console.WriteLine($"exporter.Service.TotalFireMessageCount={exporter.Service.TotalFireMessageCount}");

        Console.WriteLine("--------- received envelopes ---------");
        foreach (var envelope in client.ReceivedEnvelopes)
        {
            Console.WriteLine($"{envelope.connect_reply?.GetType().Name}{envelope.telemetry_buffer?.GetType().Name}");
        }
        Console.WriteLine("--------- end of received envelopes ---------");

        var receivedRootNode = LoadReceivedTelemetryBuffers(client);
        AssertReceivedTelemetry(receivedRootNode);
        PrintNodeTree(receivedRootNode);

        serverTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
        clientTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }

    [Test]
    public async Task CanSendAccumulatedStringMapToClient()
    {
        TestEndpointTelemetry serverTelemetry = new TestEndpointTelemetry();
        TestClientChannelTelemetry clientTelemetry;

        using var exporter = new CodePathWebSocketExporter(listenPortNumber: 3003, serverTelemetry);
        var environment = new CodePathEnvironment(CodePathLogLevel.Debug, exporter);

        var writer = new CodePathWriter(environment, "test");
        using (writer.Span("M1", CodePathLogLevel.Debug))
        {
            writer.Message("M2", CodePathLogLevel.Debug);
            writer.Message("M3", CodePathLogLevel.Debug);
        }
        await Task.Delay(100);

        var client = CreateCodePathClient(out clientTelemetry);

        await using (client)
        {
            await client.SendEnvelope(new CodePathClientToServer() {
                connect_request = new CodePathClientToServer.ConnectRequest()
            });

            await client.WaitForIncomingEnvelope(
                predicate: e => e.connect_reply != null, 
                millisecondsTimeout: 1000);

            writer.Message("M1", CodePathLogLevel.Debug);

            await client.WaitForIncomingEnvelopes(
                predicate: e => e.telemetry_buffer != null,
                count: 2,
                millisecondsTimeout: 1000);

            await client.SendEnvelope(new CodePathClientToServer {
                disconnect_request = new CodePathClientToServer.DisconnectRequest()
            });
        }

        exporter.Dispose();

        var firstTelemetryBuffer = client.ReceivedEnvelopes
            .First(e => e.telemetry_buffer != null)
            .telemetry_buffer;

        firstTelemetryBuffer.Buffer[0].Should().Be((byte) LogStreamOpCode.StringKey);

        using var firstTelemetryBufferReader = 
            new BinaryReader(new MemoryStream(firstTelemetryBuffer.Buffer), Encoding.UTF8);
        
        AssertStringKeyEntry(firstTelemetryBufferReader, 1, "M1");
        AssertStringKeyEntry(firstTelemetryBufferReader, 2, "M2");
        AssertStringKeyEntry(firstTelemetryBufferReader, 3, "M3");

        //--- debug output

        var receivedRootNode = LoadReceivedTelemetryBuffers(client);
        PrintNodeTree(receivedRootNode);

        serverTelemetry.PrintAllToConsole();
        clientTelemetry.PrintAllToConsole();

        Console.WriteLine($"exporter.TotalBufferCount={exporter.TotalBufferCount}");
        Console.WriteLine($"exporter.Service.TotalObserveBuffersCount={exporter.Service.TotalObserveBuffersCount}");
        Console.WriteLine($"exporter.Service.TotalFireMessageCount={exporter.Service.TotalFireMessageCount}");

        Console.WriteLine("--------- received envelopes ---------");
        foreach (var envelope in client.ReceivedEnvelopes)
        {
            Console.WriteLine($"{envelope.connect_reply?.GetType().Name}{envelope.telemetry_buffer?.GetType().Name}");
        }
        Console.WriteLine("--------- end of received envelopes ---------");

        serverTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
        clientTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }

    [Test]
    public async Task SendEndlessTelemetryForFrontEndDebugging()
    {
        TestEndpointTelemetry serverTelemetry = new TestEndpointTelemetry();

        using var exporter = new CodePathWebSocketExporter(listenPortNumber: 3003, serverTelemetry);
        var environment = new CodePathEnvironment(CodePathLogLevel.Debug, exporter);
        var cancelAfter10Minutes = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        await Task.Delay(5000, cancelAfter10Minutes.Token);
        await PlayTelemetryExampleForFrontEnd(environment, cancelAfter10Minutes.Token);

        exporter.Dispose();

        Console.WriteLine($"exporter.TotalBufferCount={exporter.TotalBufferCount}");
        Console.WriteLine($"exporter.Service.TotalObserveBuffersCount={exporter.Service.TotalObserveBuffersCount}");
        Console.WriteLine($"exporter.Service.TotalFireMessageCount={exporter.Service.TotalFireMessageCount}");
        Console.WriteLine($"exporter.Service.TotalTelemetryBytes={exporter.Service.TotalTelemetryBytes}");

        serverTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }

    private async Task PlayTelemetryExampleForUnitTest(CodePathEnvironment environment)
    {
        var writer = new CodePathWriter(environment, "test");

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
    }

    private async Task PlayTelemetryExampleForFrontEnd(CodePathEnvironment environment, CancellationToken cancellation)
    {
        var telemetry = new TestForFrontEndTelemetry(environment);

        await Task.Delay(3000, cancellation);
        
        try
        {
            int iteration = 0;
            var clock = Stopwatch.StartNew();
        
            while (!cancellation.IsCancellationRequested)
            {
                iteration++;
            
                telemetry.InfoHello();
                
                await Task.Delay(100, cancellation);
                
                using (var outerSpan = telemetry.SpanDoSomeWork(iteration, $"iteration#{iteration}"))
                {
                    await Task.Delay(100, cancellation);

                    if ((iteration % 3) == 0)
                    {
                        telemetry.WarnSomethingDangerous(clock.Elapsed);
                    }

                    telemetry.DebugSomeInternalDebugOne();
                    telemetry.DebugSomeInternalDebugTwo();
                    telemetry.DebugSomeInternalDebugThree();

                    for (int step = 0 ; step < 10 ; step++)
                    {
                        await Task.Delay(20, cancellation);

                        using (var innerSpan = telemetry.SpanDoSomeWorkStep(step, $"step # {step}"))
                        {
                            try
                            {
                                telemetry.DebugSomeInternalDebugFour();
                                telemetry.DebugSomeInternalDebugFive();

                                await Task.Delay(10, cancellation);

                                if ((iteration % 4) == 0 && step == 5)
                                {
                                    throw new InvalidOperationException("You cannot do this now");
                                }
                            }
                            catch (Exception e)
                            {
                                innerSpan.Fail(e);
                            }
                        }

                        if ((iteration % 7) == 0)
                        {
                            try
                            {
                                string? s = null;
                                Console.WriteLine(s!.PadLeft(100));
                            }
                            catch (Exception e)
                            {
                                telemetry.ErrorThisAllWentWrong(e);
                            }
                        }
                    }
                }
                
                await Task.Delay(500, cancellation);

                telemetry.VerboseKeepAlive(beatNo: iteration);
            }
        }
        catch (OperationCanceledException e)
        {
            telemetry.ErrorCancellationRequested(e);
        }
    }

    private void AssertStringKeyEntry(BinaryReader reader, int key, string value)
    {
        reader.ReadByte().Should().Be((byte)LogStreamOpCode.StringKey);
        reader.ReadInt32().Should().Be(key);
        reader.ReadString().Should().Be(value);
    }

    private void AssertReceivedTelemetry(CodePathStreamReader.Node root)
    {
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

    private CodePathStreamReader.Node LoadReceivedTelemetryBuffers(TestCodePathClient client)
    {
        var stream = new MemoryStream();
        stream.WriteByte((byte)LogStreamOpCode.BeginStreamChunk);
        
        var bufferMessagesQuery = client
            .ReceivedEnvelopes.Select(e => e.telemetry_buffer)
            .Where(body => body != null);
        foreach (var body in bufferMessagesQuery)
        {
            stream.Write(body.Buffer, 0, body.Buffer.Length);
        }

        stream.WriteByte((byte)LogStreamOpCode.EndStreamChunk);

        Console.WriteLine("------------ streamed contents ---------------");
        PrintBuffer(stream);
        Console.WriteLine("------------ end of streamed contents ---------------");
        
        stream.Position = 0;
        var reader = new CodePathStreamReader();
        reader.ReadLogFile(stream);

        return reader.RootNode;
    }
    
    private TestCodePathClient CreateCodePathClient(out TestClientChannelTelemetry telemetry)
    {
        var url = $"ws://localhost:3003/telemetry";
        var client = new TestCodePathClient(url);
        telemetry = client.Telemetry;
        return client;
    }
    
    private class TestForFrontEndTelemetry : ITelemetry
    {
        private static readonly string __s_hello = "Hello";
        private static readonly string __s_someLowLevelStuff = "SomeLowLevelStuff";
        private static readonly string __s_str = "str";
        private static readonly string __s_num = "num";
        private static readonly string __s_somethingDangerous = "SomethingDangerous";
        private static readonly string __s_timeLeft = "timeLeft";
        private static readonly string __s_keepAlive = "KeepAlive";
        private static readonly string __s_beatNo = "beatNo";
        private static readonly string __s_doSomeWork = "DoSomeWork";
        private static readonly string __s_inputNumber = "inputNumber";
        private static readonly string __s_inputString = "inputString";
        private static readonly string __s_doSomeWorkStep = "DoSomeWorkStep";
        private static readonly string __s_stepIndex = "stepIndex";
        private static readonly string __s_stepName = "stepName";
        private static readonly string __s_someInternalDebugOne = "someInternalDebugOne";
        private static readonly string __s_someInternalDebugTwo = "someInternalDebugTwo";
        private static readonly string __s_someInternalDebugThree = "someInternalDebugThree";
        private static readonly string __s_someInternalDebugFour = "someInternalDebugFour";
        private static readonly string __s_someInternalDebugFive = "someInternalDebugFive";
        private static readonly string __s_thisAllWentWrong = "ThisAllWentWrong";
        private static readonly string __s_cancellationRequested = "CancellationRequested";
        
        private readonly ICodePathEnvironment _environment;
        private readonly CodePathWriter _writer;

        public TestForFrontEndTelemetry(ICodePathEnvironment environment)
        {
            _environment = environment;
            _writer = new(_environment, "TestForFrontEnd");
        }
        
        public void InfoHello()
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_hello, CodePathLogLevel.Info);
            buffer.Flush();
        }

        public void DebugSomeLowLevelStuff(int num, string str)
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteBeginMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_someLowLevelStuff, CodePathLogLevel.Debug);
            buffer.WriteValue(__s_num, num);
            buffer.WriteValue(__s_str, str);
            buffer.WriteEndMessage();
            buffer.Flush();
        }

        public void WarnSomethingDangerous(TimeSpan timeLeft)
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteBeginMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_somethingDangerous, CodePathLogLevel.Warning);
            buffer.WriteValue(__s_timeLeft, timeLeft);
            buffer.WriteEndMessage();
            buffer.Flush();
        }

        public void VerboseKeepAlive(int beatNo)
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteBeginMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_keepAlive, CodePathLogLevel.Verbose);
            buffer.WriteValue(__s_beatNo, beatNo);
            buffer.WriteEndMessage();
            buffer.Flush();
        }

        public ITraceSpan SpanDoSomeWork(int inputNumber, string inputString)
        {
            _writer.SpawnNewSpan(out var spanId, out var parentSpanId);

            var buffer = _environment.NewBuffer();
            buffer.WriteBeginOpenSpan(spanId, parentSpanId, _environment.GetUtcNow(), __s_doSomeWork, CodePathLogLevel.Verbose);
            buffer.WriteValue(__s_inputNumber, inputNumber);
            buffer.WriteValue(__s_inputString, inputString);
            buffer.WriteEndOpenSpan();
            buffer.Flush();
        
            return new CodePathWriter.TraceSpan(_writer, spanId, parentSpanId);
        }

        public ITraceSpan SpanDoSomeWorkStep(int stepIndex, string stepName)
        {
            _writer.SpawnNewSpan(out var spanId, out var parentSpanId);

            var buffer = _environment.NewBuffer();
            buffer.WriteBeginOpenSpan(spanId, parentSpanId, _environment.GetUtcNow(), __s_doSomeWorkStep, CodePathLogLevel.Verbose);
            buffer.WriteValue(__s_stepIndex, stepIndex);
            buffer.WriteValue(__s_stepName, stepName);
            buffer.WriteEndOpenSpan();
            buffer.Flush();
        
            return new CodePathWriter.TraceSpan(_writer, spanId, parentSpanId);
        }

        public void DebugSomeInternalDebugOne()
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_someInternalDebugOne, CodePathLogLevel.Debug);
            buffer.Flush();
        }

        public void DebugSomeInternalDebugTwo()
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_someInternalDebugTwo, CodePathLogLevel.Debug);
            buffer.Flush();
        }

        public void DebugSomeInternalDebugThree()
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_someInternalDebugThree, CodePathLogLevel.Debug);
            buffer.Flush();
        }

        public void DebugSomeInternalDebugFour()
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_someInternalDebugFour, CodePathLogLevel.Debug);
            buffer.Flush();
        }

        public void DebugSomeInternalDebugFive()
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_someInternalDebugFive, CodePathLogLevel.Debug);
            buffer.Flush();
        }

        public void ErrorThisAllWentWrong(Exception error)
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteBeginMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_thisAllWentWrong, CodePathLogLevel.Error);
            buffer.WriteException(error);
            buffer.WriteEndMessage();
            buffer.Flush();
        }

        public void ErrorCancellationRequested(Exception error)
        {
            var buffer = _environment.NewBuffer();
            buffer.WriteBeginMessage(_environment.GetCurrentSpanId(), _environment.GetUtcNow(), __s_cancellationRequested, CodePathLogLevel.Error);
            buffer.WriteException(error);
            buffer.WriteEndMessage();
            buffer.Flush();
        }
    }
}
