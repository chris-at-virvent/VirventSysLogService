using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirventPluginContract;

namespace VirventSysLogServerEngine.ThreadHelpers
{
    public class ProcessPluginThread
    {
        private Plugin Plugin;
        public List<PluginMessage> PluginMessages;
        private Engine Engine;

        public ProcessPluginThread(Plugin plugin, Engine engine)
        {
            Plugin = plugin;
            PluginMessages = new List<PluginMessage>();
            Engine = engine;
        }

        public void Process()
        {
            Plugin.PluginAssembly.Run(Plugin.Settings, out PluginMessages);
            if (PluginMessages.Count != 0)
            {
                Engine.dataConnection = Data.GetConnection(Engine.connectionString);

                foreach (var item in PluginMessages)
                {
                    Engine.LogApplicationActivity(item.msg, item.severity, item.facility, Engine.dataConnection);
                }
            }
        }
    }

    public delegate void ProcessCallback(List<PluginMessage> messages);

}
