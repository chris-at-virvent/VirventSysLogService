using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventPluginContract
{
    public class Settings
    {
        public static string GetSetting(List<PluginSetting> objSource, string key)
        {
            foreach (var item in objSource)
            {
                if (item.Key == key)
                {
                    return item.Value.ToString();
                }
            }
            return null;
        }
    }
    public static class Extensions
    {
        public static string[] Rfc3339DateTimePatterns
        {
            get
            {
                var formats = new string[11];

                // Rfc3339DateTimePatterns
                formats[0] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK";
                formats[1] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffK";
                formats[2] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffK";
                formats[3] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffK";
                formats[4] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK";
                formats[5] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffK";
                formats[6] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fK";
                formats[7] = "yyyy'-'MM'-'dd'T'HH':'mm':'ssK";

                // Fall back patterns
                formats[8] = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"; // RoundtripDateTimePattern
                formats[9] = DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern;
                formats[10] = DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern;

                return formats;
            }
        }

        public static string ToRfc3339String(this System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'zzz", DateTimeFormatInfo.InvariantInfo);
        }

        public static DateTime FromRfc3339String(this string dateTime)
        {
            DateTime parseResult;
            if (DateTime.TryParseExact(dateTime, Rfc3339DateTimePatterns, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal, out parseResult))
            {
                parseResult = DateTime.SpecifyKind(parseResult, DateTimeKind.Local);
                return parseResult;
            }
                
            return DateTime.MinValue;
        }

    }
}
