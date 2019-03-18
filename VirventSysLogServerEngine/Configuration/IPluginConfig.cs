using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventSysLogServerEngine.Configuration
{
    public interface IPluginConfiguration
    {
        IEnumerable<IPluginSetting> PluginSettings { get; }
    }

    public interface IPluginSetting
    {
        string Name { get; }
        int Hours { get; }
        int Minutes { get; }
        int Seconds { get; }
        IEnumerable<ISetting> Settings { get; }

    }

    public interface ISetting
    {
        string Key { get; }
        string Value { get; }
    }

}
