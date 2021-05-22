using System;
using System.Collections.Generic;
using System.IO;

namespace Atc.Data.Primitives
{
    public readonly struct GeoRect
    {
        public GeoRect(GeoPoint min, GeoPoint max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(GeoPoint location)
        {
            return (
                location.Lat >= Min.Lat && 
                location.Lat <= Max.Lat && 
                location.Lon >= Min.Lon && 
                location.Lon <= Max.Lon);
        }
        
        public readonly GeoPoint Min;
        public readonly GeoPoint Max;
    }
}
