using System.Collections.Generic;
using VirventDataContract;
using VirventPluginContract;

namespace VirventSysLogServerEngine.ThreadHelpers
{
    public class ProcessPluginThread
    {
        private Plugin Plugin;
        public List<PluginMessage> PluginMessages;
        private Engine Engine;
        private Message Message;

        public ProcessPluginThread(Plugin plugin, Engine engine, Message message)
        {
            Plugin = plugin;
            PluginMessages = new List<PluginMessage>();
            Engine = engine;
            Message = message;
        }

        public void Process()
        {
            Plugin.PluginAssembly.Run(Plugin.Settings, Message, out PluginMessages);
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
