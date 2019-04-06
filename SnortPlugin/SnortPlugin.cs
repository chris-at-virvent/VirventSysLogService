using System.Collections.Generic;
using VirventPluginContract;

namespace SnortPlugin
{
    public class Main : IPlugin
    {
        public SnortSettings settings;

        public string Name
        {
            get
            {
                return "SnortPlugin";
            }
        }

        public void Run(List<PluginSetting> Settings, Message message, out List<PluginMessage> Responses)
        {
            List<PluginMessage> responses = new List<PluginMessage>();
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            settings = new SnortSettings();
            settings.DownloadPath = SettingsHelper.GetSetting(Settings, "DownloadPath");
            settings.OinkCode = SettingsHelper.GetSetting(Settings, "OinkCode");
            settings.SnortPath = SettingsHelper.GetSetting(Settings, "SnortPath");
            settings.SnortRemoteServer = SettingsHelper.GetSetting(Settings, "SnortRemoteServer");
            settings.SnortSubscrptionLevel = SettingsHelper.GetSetting(Settings, "SnortSubscrptionLevel");
            settings.SnortVersion = SettingsHelper.GetSetting(Settings, "SnortVersion");
            settings.UnpackPath = SettingsHelper.GetSetting(Settings, "UnpackPath");

            int ServiceCount = int.Parse(SettingsHelper.GetSetting(Settings, "ServiceCount"));
            if (ServiceCount > 0)
                settings.ServiceEndpoints = new List<string>();

            for (var i = 1; i < ServiceCount; i++)
            {
                settings.ServiceEndpoints.Add(SettingsHelper.GetSetting(Settings, "snort" + i));
            }

            pluginMessages = new List<PluginMessage>();
            SnortDefinitionDownloader.GetDefinitions(settings, out pluginMessages);
            responses.AddRange(pluginMessages);

            pluginMessages = new List<PluginMessage>();
            SnortDefinitionDownloader.SnortRewrite(settings, out pluginMessages);
            responses.AddRange(pluginMessages);

            foreach (var item in settings.ServiceEndpoints)
            {
                pluginMessages = new List<PluginMessage>();
                SnortDefinitionDownloader.RestartService(item, out pluginMessages);
            }

            Responses = responses;
        }
    }
}