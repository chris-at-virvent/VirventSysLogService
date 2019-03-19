using System.Collections.Generic;
using VirventPluginContract;

namespace SnortPlugin.Helpers
{
    public class SettingsHelper
    {
        /// <summary>
        /// Object Mapper
        /// </summary>
        /// <param name="snortSettings">The object to transpose on to</param>
        /// <param name="objSource">The object to be transposed</param>
        /// <returns>The object to transpose to</returns>
        /// <remarks></remarks>
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

}
