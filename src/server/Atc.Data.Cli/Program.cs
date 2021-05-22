using System;
using Atc.Data.Traffic;

namespace Atc.Data.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new AtccAptDatReadTest();
            test.ReadRealAirport_HUEN();
            test.ReadRealAirport_HUEN();
            test.ReadRealAirport_HUEN();
        }
    }
}