using System;

namespace Atc.Data.Primitives
{
    public static class NumericExtensions
    {
        public static int RoundToInt32(this float value)
        {
            return (int)Math.Round(value);
        }
    }
}
