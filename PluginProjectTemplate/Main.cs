using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirventPluginContract;

namespace PluginProjectTemplate
{
    public class Main : IPlugin
    {
        public string Name
        {
            get
            {
                return "MyPlugin";
            }
        }

        public void Run(List<PluginSetting> Settings, Message message, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            Responses = pluginMessages;
        }
    }
}
