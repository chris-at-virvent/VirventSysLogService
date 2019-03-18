using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirventPluginContract;

namespace ProcessCheckerPlugin
{
    public class Main : VirventPluginContract.IPlugin
    {
        public string Name
        {
            get
            {
                return "ProcessCheckerPlugin";
            }
        }

        public void Run(List<PluginSetting> Settings, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            var checkedProcesses = CheckRunningProcesses.CheckProcess(Settings);
            if (checkedProcesses != null)
            {
                foreach (var i in checkedProcesses)
                {
                    if (i.ProcessIsRunning)
                    {
                        PluginMessage message = new PluginMessage();
                        message.severity = Severities.Informational;
                        message.facility = Facilities.log_audit;
                        message.msg = i.Process.ProcessName + " operational.";

                        pluginMessages.Add(message);
                    }
                    else
                    {
                        PluginMessage message = new PluginMessage();
                        message.severity = Severities.Emergency;
                        message.facility = Facilities.kernel_messages;
                        message.msg = i.ProcessToCheck + " not loaded.";

                        pluginMessages.Add(message);
                    }
                }
            }
            Responses = pluginMessages;
        }

    }
}
