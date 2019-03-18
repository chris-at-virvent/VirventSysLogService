using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VirventPluginContract;

namespace ProcessCheckerPlugin
{
    public class CheckRunningProcesses
    {
        public static List<ProcessesToCheck> CheckProcess(List<PluginSetting> settings)
        {
            List<ProcessesToCheck> processesChecked = new List<ProcessesToCheck>();
            List<string> processesToCheck = new List<string>();

            foreach (var item in settings)
            {
                processesToCheck.Add(item.Key);
            }


            var processList = Process.GetProcesses();
            foreach (var i in processList)
            {
                if (processesToCheck.Contains(i.ProcessName.ToLower()))
                {
                    processesToCheck.Remove(i.ProcessName.ToLower());
                    processesChecked.Add(new ProcessesToCheck() { ProcessToCheck = i.ProcessName.ToLower(), ProcessIsRunning = true, Process = i });
                }
            }

            if (processesToCheck.Count != 0)
            {
                foreach (var i in processesToCheck)
                {
                    processesChecked.Add(new ProcessesToCheck { ProcessToCheck = i, ProcessIsRunning = false, Process = null });
                }
            }

            return processesChecked;
        }

    }

    public class ProcessesToCheck
    {
        public string ProcessToCheck { get; set; }
        public bool ProcessIsRunning { get; set; }
        public Process Process { get; set; }
    }

}
