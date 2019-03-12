using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
// using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Configuration;
using System.Threading;
using System.Timers;
using VirventSysLogServerEngine;

namespace SyslogServerTestHarness
{
    class VirventSysLog
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Server");
            Engine engine = new Engine();

            while(true)
            {

            }

        }
    }
}
