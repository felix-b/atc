using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zero.Doubt.Logging.Viewer
{
    class Program
    {
        private const string listenUrl = "http://localhost:9003";
        
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("call: zdlv <path_to_zdl_file>");
                return 1;
            }

            try
            {
                Console.WriteLine("loading log file.");
                
                using var file = File.OpenRead(args[0]);
                var reader = new BinaryLogStreamReader(file);
                reader.ReadToEnd();

                Console.WriteLine($"load success, {reader.RootNode.Nodes.Count} nodes on the root level");
                Console.WriteLine("starting web server.");

                var host = WebHost
                    .CreateDefaultBuilder()
                    .ConfigureServices(services => {
                        services.AddControllers();
                        services.AddSingleton(reader);
                    })
                    .Configure(app => {
                        app.UseStaticFiles("/static");
                        app.UseRouting();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapControllers();
                        });
                    })
                    .UseWebRoot("static")
                    .UseUrls(listenUrl)
                    .Build();

                host.Start();
                
                Console.WriteLine($"web server started");
                Console.WriteLine($"to access, browse to: {listenUrl}");

                host.WaitForShutdown();
                
                Console.WriteLine($"web server down.");
                
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"FAILURE! {e.GetType().Name}: {e.Message}");
                Console.ReadLine();
                return 2;
            }
        }
    }
}
