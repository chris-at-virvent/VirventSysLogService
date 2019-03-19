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
        public static string ToRfc3339String(this System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'zzz", DateTimeFormatInfo.InvariantInfo);
        }
    }
}
