using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventSysLogServerEngine
{
    public class ChildProcessChecker
    {      
        public static List<ProcessesToCheck> CheckProcess()
        {
            List<ProcessesToCheck> processesChecked = new List<ProcessesToCheck>();
            List<string> processesToCheck = new List<string>();

            VirventSysLogLibrary.Configuration.MultipleValuesSection multipleValuesSection = (VirventSysLogLibrary.Configuration.MultipleValuesSection)ConfigurationManager.GetSection("ProcessesToMonitor");
            foreach (VirventSysLogLibrary.Configuration.ValueElement item in multipleValuesSection.Values)
            {
                processesToCheck.Add(item.Name.ToLower());                
            }

            var processList = Process.GetProcesses();
            foreach (var i in processList)
            {
                if (processesToCheck.Contains(i.ProcessName.ToLower()))
                {
                    processesToCheck.Remove(i.ProcessName.ToLower());
                    processesChecked.Add(new ProcessesToCheck() { ProcessToCheck = i.ProcessName.ToLower(), ProcessIsRunning=true, Process = i });
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
