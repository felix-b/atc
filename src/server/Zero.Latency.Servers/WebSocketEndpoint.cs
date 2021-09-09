using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Zero.Latency.Servers
{
    public class WebSocketEndpoint : IAsyncDisposable
    {
        private readonly string _urlPath;
        private readonly int _port;
        private readonly ISocketAcceptor _socketAcceptor;
        private readonly IHost _host;
        private readonly CancellationTokenSource _disposed = new();

        public WebSocketEndpoint(int port, string urlPath, ISocketAcceptor socketAcceptor)
        {
            _urlPath = urlPath.StartsWith("/") ? urlPath : "/" + urlPath;
            _port = port;
            _socketAcceptor = socketAcceptor;
            _host = CreateHost();
        }

        public async ValueTask DisposeAsync()
        {
            _disposed.Cancel(true);
            Console.WriteLine("WebSocketEndpoint.DisposeAsync:0");
            await _socketAcceptor.DisposeAsync();
            Console.WriteLine("WebSocketEndpoint.DisposeAsync:1");
            await _host.StopAsync(TimeSpan.FromSeconds(10));
            Console.WriteLine("WebSocketEndpoint.DisposeAsync:2");
        }

        public void Run()
        {
            _host.Run();
        }

        public Task StartAsync()
        {
            return _host.StartAsync(_disposed.Token);
        }

        public Task RunAsync()
        {
            return _host.RunAsync(_disposed.Token);
        }

        public Task WaitForShutdownAsync()
        {
            return _host.WaitForShutdownAsync(_disposed.Token);
        }

        public Task StopAsync(TimeSpan timeout)
        {
            Console.WriteLine("WebSocketEndpoint.StopAsync:0");
            try
            {
                return _host.StopAsync(timeout);
            }
            finally
            {
                Console.WriteLine("WebSocketEndpoint.StopAsync:1");
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
