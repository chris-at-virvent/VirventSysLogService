using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirventPluginContract;

namespace SOC2GitHubProcedureScheduler
{
    public class Main : IPlugin
    {
        public string Name
        {
            get
            {
                return "SOC2GitHub";
            }
        }
        public void Run(List<PluginSetting> settings, Message message, out List<PluginMessage> messages)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();
            var proc = new SOC2GitHubProcedureScheduler();
            proc.ProcessTasks(settings, out pluginMessages);
            messages = pluginMessages;
        }
    }
}
