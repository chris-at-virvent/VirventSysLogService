using System.Collections.Generic;
using System.Data.SqlClient;
using VirventDataContract;
using VirventPluginContract;

namespace WindowsFirewallAutoRulePlugin
{
    public class Main : IPlugin
    {
        public string Name
        {
            get
            {
                return "WindowsFirewallAutoBan";
            }
        }

        public void Run(List<PluginSetting> Settings, Message message, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            // custom settings                                    
            // "threshold" int [0-7] - what is the severity threshold for an autoban event?
            Severities Threshold = (Severities)int.Parse(SettingsHelper.GetSetting(Settings, "Threshold"));
            // "count" int [0-9999]  - what is the count threshold for an autoban event?
            int Count = int.Parse(SettingsHelper.GetSetting(Settings, "Count"));
            // a setting of 0 means do not query for number of events

            // timespan threshold
            // hours int [0-24]
            int Hours = int.Parse(SettingsHelper.GetSetting(Settings, "Hours"));
            // minutes int [0-60]
            int Minutes = int.Parse(SettingsHelper.GetSetting(Settings, "Minutes"));
            // seconds int [0-60]
            int Seconds = int.Parse(SettingsHelper.GetSetting(Settings, "Seconds"));

            // "event" string        - the original snort message            
            // get the message from the event - this is passed under the setting key of "event"

            // parse the message and look to see if this is worthy of a ban
            // setting here is "threshold"
            bool banAddress = false;
            if (message.Severity <= Threshold)
            {
                if (Count == 0)
                    banAddress = true;

                if (!banAddress)
                {
                    SqlConnection connection = Data.GetConnection(SettingsHelper.GetSetting(Settings, "SysLogConnString"));
                    int foundEntries = Data.GetEntries(connection, (int)message.Severity, message.Host,  message.SourceIP, message.Received.AddHours(Hours*-1).AddMinutes(Minutes*-1).AddSeconds(Seconds*-1));
                    if (foundEntries >= Count)
                        banAddress = true;
                }

                if (banAddress)
                    if (!FirewallHandler.CheckForRule(message.SourceIP))
                    {
                        FirewallHandler.CreateBlockRule(message.SourceIP);
                        PluginMessage pluginMessage = new PluginMessage();
                        pluginMessage.severity = Severities.Informational;
                        pluginMessage.facility = Facilities.log_audit;
                        pluginMessage.msg = "Firewall -> AutoBan Block Rule Created For " + message.SourceIP;

                        pluginMessages.Add(pluginMessage);
                    }
            }

            // if count > threshold, this is a block

            //var checkedProcesses = CheckRunningProcesses.CheckProcess(Settings);
            //if (checkedProcesses != null)
            //{
            //    foreach (var i in checkedProcesses)
            //    {
            //        if (i.ProcessIsRunning)
            //        {
            //            PluginMessage message = new PluginMessage();
            //            message.severity = Severities.Informational;
            //            message.facility = Facilities.log_audit;
            //            message.msg = i.Process.ProcessName + " operational.";

            //            pluginMessages.Add(message);
            //        }
            //        else
            //        {
            //            PluginMessage message = new PluginMessage();
            //            message.severity = Severities.Emergency;
            //            message.facility = Facilities.kernel_messages;
            //            message.msg = i.ProcessToCheck + " not loaded.";

            //            pluginMessages.Add(message);
            //        }
            //    }
            //}
            Responses = pluginMessages;
        }
    }
}
