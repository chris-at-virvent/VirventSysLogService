using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirventPluginContract;

namespace SOC2GitHubProcedureScheduler
{
    public class SOC2GitHubProcedureScheduler
    {
        public List<SOC2Procedure> procedures;

        public void ProcessTasks(List<PluginSetting> Settings, out List<PluginMessage> pluginMessages)
        {
            procedures = new List<SOC2Procedure>();
            List<PluginMessage> responseMessages = new List<PluginMessage>();
            // set the path and load all files
            string[] templates = Directory.GetFiles(SettingsHelper.GetSetting(Settings, "ProcedureTemplateDirectory"));
            string oAuthToken = SettingsHelper.GetSetting(Settings, "GitKey");
            string repo = SettingsHelper.GetSetting(Settings, "Repo");
            string gituser = SettingsHelper.GetSetting(Settings, "User");

            // iterate files
            foreach (var template in templates)
            {
                procedures.Add(new SOC2Procedure(SettingsHelper.GetSetting(Settings, "ProcedureTemplateDirectory"), template));
            }

            // iterate templates
            foreach (SOC2Procedure procedure in procedures)
            {
                if (procedure.Runnable())
                {
                    // fire git integration to create the task
                    GitHub.SendTaskToGit(oAuthToken, repo, gituser, procedure);
                    // update last run date to now
                    procedure.LastRun = DateTime.Now;
                    // save task data
                    procedure.Save();
                }
            }

            pluginMessages = responseMessages;
        }

    }
}
