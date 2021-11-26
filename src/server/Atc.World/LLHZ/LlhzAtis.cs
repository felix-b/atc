using System;
using Atc.Data.Primitives;

namespace Atc.World.LLHZ
{
    public record LlhzAtis(
        string Info, // letter A-Z
        string ActiveRunway,
        Pressure Qnh,
        Bearing? WindBearing, // null means variable
        Speed WindSpeed,
        Speed? WindGust)
    {
        public static LlhzAtis CreateRandom(DateTime utcNow)
        {
            //TODO
            return new LlhzAtis(
                Info: "Z",
                ActiveRunway: "29",
                Qnh: Pressure.Qne,
                WindBearing: null,
                WindSpeed: Speed.FromKnots(5),
                WindGust: null);
        }
    }
}
