using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zero.Doubt.Logging;

namespace Zero.Latency.Servers
{
    public class WebSocketEndpoint : IAsyncDisposable
    {
        private readonly string _urlPath;
        private readonly int _port;
        private readonly ISocketAcceptor _socketAcceptor;
        private readonly IEndpointLogger _logger;
        private readonly IHost _host;
        private readonly CancellationTokenSource _disposed = new();

        public WebSocketEndpoint(int port, string urlPath, ISocketAcceptor socketAcceptor, IEndpointLogger logger)
        {
            _urlPath = urlPath.StartsWith("/") ? urlPath : "/" + urlPath;
            _port = port;
            _socketAcceptor = socketAcceptor;
            _logger = logger;
            _host = CreateHost();
        }

        public async ValueTask DisposeAsync()
        {
            _disposed.Cancel(true);

            _logger.EndpointDisposingAsync(step: 1);
            
            await _socketAcceptor.DisposeAsync();
            
            _logger.EndpointDisposingAsync(step: 2);
            
            await _host.StopAsync(TimeSpan.FromSeconds(10));
            
            _host.Dispose();
            
            _logger.EndpointDisposingAsync(step: 3);
        }

        public void Run()
        {
            _logger.HostRunStarting(); 
            _host.Run();
            _logger.HostRunFinished(); 
        }

        public Task StartAsync()
        {
            _logger.HostRunStarting(); 
            return _host.StartAsync(_disposed.Token);
        }

        public Task RunAsync()
        {
            _logger.HostRunStarting(); 
            return _host.RunAsync(_disposed.Token);
        }

        public Task WaitForShutdownAsync()
        {
            return _host.WaitForShutdownAsync(_disposed.Token);
        }

        public Task StopAsync(TimeSpan timeout)
        {
            _logger.HostStoppingAsync(step: 0);
            try
            {
                return _host.StopAsync(timeout);
            }
            finally
            {
                _logger.HostStoppingAsync(step: 1);
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
                })
                .Build();
            
            return host;
        }

        private void ConfigureWebSocket(WebHostBuilderContext hostContext, IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            
            app.UseWebSockets();
            app.Use(async (context, next) => {
                LogEngine.BranchAsyncTask($"asp.net.core incoming");
                    
                if (context.Request.Path == _urlPath)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var socket = await context.WebSockets.AcceptWebSocketAsync();
                        await _socketAcceptor.AcceptSocket(context, socket, cancel: lifetime!.ApplicationStopping);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
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
}
