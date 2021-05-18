using System;
using Atc.Data.Traffic;

namespace Atc.Data.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var x = new AircraftData();
            Console.WriteLine(x);
        }
    }
}