using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atc.Server
{
    public class ServiceHostBuilder
    {
        private readonly IWebSocketService _service;

        public ServiceHostBuilder(IWebSocketService service)
        {
            _service = service;
        }

        public IHost CreateHost()
        {
            var host = Host
                .CreateDefaultBuilder()
                .ConfigureWebHostDefaults(hostBuilder => {
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
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var socket = await context.WebSockets.AcceptWebSocketAsync();
                        await _service.AcceptConnection(socket, cancel: lifetime!.ApplicationStopping);
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
    }
}
