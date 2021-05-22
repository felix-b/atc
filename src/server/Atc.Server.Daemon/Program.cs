using System;
using Microsoft.Extensions.Hosting;

namespace Atc.Server.Daemon
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var hostBuilder = new ServiceHostBuilder(new EchoService());
            var host = hostBuilder.CreateHost();
            host.Run();
            Console.WriteLine("Goodbye World!");
        }
    }
}
