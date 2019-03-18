using System;
using System.Globalization;

namespace VirventSysLogServerEngine.Extensions
{
    public static class Extensions
    {
        private static readonly System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime SqlMin(this System.DateTime dt)
        {
            dt = System.DateTime.Parse("01/01/1753");
            return dt;
        }

        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static long ToUnixTime(this System.DateTime date)
        {
            var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }
        public static string ToRfc3339String(this System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'zzz", DateTimeFormatInfo.InvariantInfo);
        }
    }
}
