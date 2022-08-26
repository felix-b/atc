using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atc.Server;

public class WebSocketEndpoint : IAsyncDisposable
{
    private readonly string _urlPath;
    private readonly int _port;
    private readonly ISocketAcceptor _socketAcceptor;
    private readonly IEndpointTelemetry _telemetry;
    private readonly IHost _host;
    private readonly CancellationTokenSource _disposed = new();

    public WebSocketEndpoint(int port, string urlPath, ISocketAcceptor socketAcceptor, IEndpointTelemetry telemetry)
    {
        _urlPath = urlPath.StartsWith("/") ? urlPath : "/" + urlPath;
        _port = port;
        _socketAcceptor = socketAcceptor;
        _telemetry = telemetry;
        _host = CreateHost();
    }

    public async ValueTask DisposeAsync()
    {
        _telemetry.VerboseEndpointDisposingAsync(step: 0);

        _disposed.Cancel(true);

        _telemetry.VerboseEndpointDisposingAsync(step: 1);
            
        await _socketAcceptor.DisposeAsync();
            
        _telemetry.VerboseEndpointDisposingAsync(step: 2);
            
        await _host.StopAsync(TimeSpan.FromSeconds(10));
            
        _host.Dispose();
            
        _telemetry.VerboseEndpointDisposingAsync(step: 3);
    }

    public void Run()
    {
        _telemetry.InfoHostRunStarting(); 
        _host.Run();
        _telemetry.InfoHostRunFinished(); 
    }

    public Task StartAsync()
    {
        _telemetry.InfoHostRunStarting(); 
        return _host.StartAsync(_disposed.Token);
    }

    public Task RunAsync()
    {
        _telemetry.InfoHostRunStarting(); 
        return _host.RunAsync(_disposed.Token);
    }

    public Task WaitForShutdownAsync()
    {
        return _host.WaitForShutdownAsync(_disposed.Token);
    }

    public async Task StopAsync(TimeSpan timeout)
    {
        _telemetry.InfoHostStoppingAsync(step: 0);
        
        try
        {
            await _host.StopAsync(timeout);
        }
        finally
        {
            _telemetry.InfoHostStoppingAsync(step: 1);
        }
    }

    public IHost Host => _host;
        
    private IHost CreateHost()
    {
        var host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(hostBuilder => {
                hostBuilder.UseUrls($"http://localhost:{_port}/");
                hostBuilder.Configure(ConfigureWebSocket);
                hostBuilder.ConfigureServices(services => {
                    // now the host app is responsible for handling SIGINT/SIGTERM/...
                    services.AddSingleton<IHostLifetime, NoopHostLifetime>();
                });
            })
            .Build();
            
        return host;
    }

    private void ConfigureWebSocket(WebHostBuilderContext hostContext, IApplicationBuilder app)
    {
        var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            
        app.UseWebSockets();
        app.Use(async (context, next) => {
            if (context.Request.Path == _urlPath)
            {
                using var span = _telemetry.SpanAspNetCoreIncoming();

                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var socket = await context.WebSockets.AcceptWebSocketAsync();
                    await _socketAcceptor.AcceptSocket(context, socket, cancel: lifetime!.ApplicationStopping);
                    _telemetry.DebugAcceptSocketExited(socketState: socket.State);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    _telemetry.ErrorBadRequest();
                }
            }
            else
            {
                await next();
            }
        });
    }

    public static WebSocketEndpointBuilder Define() => new WebSocketEndpointBuilder();

    // public static WebSocketEndpoint Build<TMessageIn, TMessageOut, TDiscriminatorIn>(
    //     object serviceInstance,
    //     Func<TMessageIn, TDiscriminatorIn> extractDiscriminator,
    //     int port = 80, 
    //     string urlPath = "/ws"
    // )
    //     where TMessageIn : class
    //     where TMessageOut : class
    //     where TDiscriminatorIn : Enum
    // {
    //     var operationDispatcher = new MethodInvocationOperationDispatcher<TMessageIn, TMessageOut, TDiscriminatorIn>(
    //         serviceInstance,
    //         extractDiscriminator
    //     );
    //     var messageSerializer = new ProtobufEnvelopeSerializer<TMessageIn>();
    //     var connectionManager = new SocketConnectionManager(messageSerializer, operationDispatcher);
    //     
    //     var endpoint = new WebSocketEndpoint(port, urlPath, connectionManager);
    //     return endpoint;
    // }
}