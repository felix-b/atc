using System;

namespace Atc.Data.Primitives
{
    public static class DateTimeExtensions
    {
        private static readonly double _toLocalFactor = 12.0 / 180.0;
        
        //TODO: test
        public static DateTime ToLocalTimeAt(this DateTime utc, GeoPoint location)
        {
            var hoursOffset = (int)Math.Truncate(location.Lon * _toLocalFactor); //TODO: cache
            var adjustedUtc = utc.Add(TimeSpan.FromHours(hoursOffset));
            return new DateTime(adjustedUtc.Ticks, DateTimeKind.Local);
        }
    }
}
