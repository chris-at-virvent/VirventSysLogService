using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventSysLogServerEngine
{
    public class ChildProcessChecker
    {
        public static Process CheckProcess(string processName)
        {
            var processList = Process.GetProcesses();
            foreach (var i in processList)
            {
                if (i.ProcessName.ToLower().Contains(processName))
                {
                    return i;
                }
            }

            return null;
        }
    }
}
