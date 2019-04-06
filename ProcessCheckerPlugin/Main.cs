using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirventPluginContract;

namespace ProcessCheckerPlugin
{
    public class Main : IPlugin
    {
        public string Name
        {
            get
            {
                return "ProcessCheckerPlugin";
            }
        }

        public void Run(List<PluginSetting> Settings, Message message, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            var checkedProcesses = CheckRunningProcesses.CheckProcess(Settings);
            if (checkedProcesses != null)
            {
                foreach (var i in checkedProcesses)
                {
                    if (i.ProcessIsRunning)
                    {
                        PluginMessage pluginMessage = new PluginMessage();
                        pluginMessage.severity = Severities.Informational;
                        pluginMessage.facility = Facilities.log_audit;
                        pluginMessage.msg = i.Process.ProcessName + " operational.";

                        pluginMessages.Add(pluginMessage);
                    }
                    else
                    {
                        PluginMessage pluginMessage = new PluginMessage();
                        pluginMessage.severity = Severities.Emergency;
                        pluginMessage.facility = Facilities.kernel_messages;
                        pluginMessage.msg = i.ProcessToCheck + " not loaded.";

                        pluginMessages.Add(pluginMessage);
                    }
                }
            }
            Responses = pluginMessages;
        }

    }
}
