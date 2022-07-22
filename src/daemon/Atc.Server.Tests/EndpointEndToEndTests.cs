using System;
using System.Threading.Tasks;
using Atc.Server.TestDoubles;
using Atc.Server.Tests.Samples;
using AtcServerSamplesProto;
using NUnit.Framework;
using TestClientOne = Atc.Server.TestDoubles.TestServiceClient<AtcServerSamplesProto.Sample1ClientToServer, AtcServerSamplesProto.Sample1ServerToClient>;

namespace Atc.Server.Tests;

[TestFixture]
public class EndpointEndToEndTests
{
    private const int ServicePortNumber = 3333;
    private const string ServiceUrlPath = "/ws";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        TestServiceClientAssert.Fail = Assert.Fail;
        TestServiceClientAssert.NotNull = Assert.NotNull;
        TestServiceClientAssert.IsTrue = Assert.IsTrue;
    }
    
    [Test]
    public async Task CanStartAndStopEndpoint()
    {
        await using var endpoint = CreateSampleServiceOneEndpoint(out _, out var telemetry);

        await endpoint.StartAsync();
        await Task.Delay(250);
        await endpoint.StopAsync(TimeSpan.FromSeconds(3));
        
        telemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }

    [Test]
    public async Task CanCommunicateWithSingleClient()
    {
        TestEndpointTelemetry serverTelemetry;
        TestClientChannelTelemetry clientTelemetry;
        
        await using (var endpoint = CreateSampleServiceOneEndpoint(out _, out serverTelemetry))
        {
            await endpoint.StartAsync();

            await using (var client = CreateSampleServiceOneClient(out clientTelemetry))
            {
                await client.SendEnvelope(new Sample1ClientToServer {
                    hello_request = new Sample1ClientToServer.HelloRequest {
                        Name = "ABC",
                        InitialCounterValue = 123
                    }
                });

                await client.WaitForIncomingEnvelope(e => e.greeting_reply != null, 1000);

                await client.SendEnvelope(new Sample1ClientToServer {
                    query_counter_request = new Sample1ClientToServer.QueryCounterRequest()
                });

                await client.WaitForIncomingEnvelope(e => e.query_counter_reply != null, 1000);
            }

            await endpoint.StopAsync(TimeSpan.FromSeconds(3));
        }

        serverTelemetry.PrintAllToConsole();
        clientTelemetry.PrintAllToConsole();

        serverTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
        clientTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }

    [Test]
    public async Task CanDropClientsGracefullyOnShutdown()
    {
        TestEndpointTelemetry serverTelemetry;
        TestClientChannelTelemetry clientTelemetry;

        WebSocketEndpoint? endpoint = null;
        TestClientOne? client = null;

        try
        {
            endpoint = CreateSampleServiceOneEndpoint(out _, out serverTelemetry);
            await endpoint.StartAsync();

            client = CreateSampleServiceOneClient(out clientTelemetry);
            await client.SendEnvelope(new Sample1ClientToServer {
                hello_request = new Sample1ClientToServer.HelloRequest {
                    Name = "ABC",
                    InitialCounterValue = 123
                }
            });
            
            await client.WaitForIncomingEnvelope(e => e.greeting_reply != null, 1000);
        }
        finally
        {
            try
            {
                if (endpoint != null)
                {
                    await endpoint.DisposeAsync();
                }
            }
            finally
            {
                if (client != null)
                {
                    await client.DisposeAsync();
                }
            }
        }

        serverTelemetry.PrintAllToConsole();
        clientTelemetry.PrintAllToConsole();

        serverTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
        clientTelemetry.VerifyNoErrorsNoWarningsOr(Assert.Fail);
    }

    private WebSocketEndpoint CreateSampleServiceOneEndpoint(
        out IServiceTaskSynchronizer taskSynchronizer,
        out TestEndpointTelemetry telemetry)
    {
        var service = new SampleServiceOne();
        telemetry = new TestEndpointTelemetry();
            
        var endpoint = WebSocketEndpoint
            .Define()
            .ReceiveMessagesOfType<AtcServerSamplesProto.Sample1ClientToServer>()
            .WithDiscriminator(m => m.PayloadCase)
            .SendMessagesOfType<AtcServerSamplesProto.Sample1ServerToClient>()
            .ListenOn(ServicePortNumber, urlPath: ServiceUrlPath)
            .BindToServiceInstance(service)
            .Create(telemetry, out taskSynchronizer);

        return endpoint;
    }

    private TestClientOne CreateSampleServiceOneClient(out TestClientChannelTelemetry telemetry)
    {
        var client = new TestClientOne($"ws://localhost:{ServicePortNumber}{ServiceUrlPath}");
        telemetry = client.Telemetry;
        return client;
    }
}
