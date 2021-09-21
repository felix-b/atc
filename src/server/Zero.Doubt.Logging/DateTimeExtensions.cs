using System;

namespace Zero.Doubt.Logging
{
    public static class DateTimeExtensions
    {
        private const Int64 UnixEpochSeconds = 62_135_596_800;
        
        /// <summary>
        /// Converts a DateTime to Unix timestamp, which is number of seconds since 1 Jan 1970 (the milliseconds are dropped).  
        /// </summary>
        public static Int64 ToUnixTime(this DateTime time)
        {
            long seconds = time.Ticks / TimeSpan.TicksPerSecond;
            return seconds - UnixEpochSeconds;
        }
    }
}
