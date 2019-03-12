using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Configuration;
using VirventSysLogServerEngine;

namespace VirventSysLogService
{

    public partial class Service1 : ServiceBase
    {

        public Engine engine;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Engine engine = new Engine();
            engine.LogApplicationActivity("Service Started Successfully");
            base.OnStart(args);
        }

        protected override void OnContinue()
        {
            Engine engine = new Engine();
            engine.LogApplicationActivity("Service Resumed Successfully");
            base.OnContinue();
        }

        protected override void OnPause()
        {
            engine.LogApplicationActivity("Service Paused - Logging Is Disabled", VirventSysLogLibrary.SysLogMessage.Severities.Warning, VirventSysLogLibrary.SysLogMessage.Facilities.log_alert);
            engine = null;
            base.OnPause();
        }

        protected override void OnStop()
        {
            engine.LogApplicationActivity("Service Stopped - Logging Is Disabled", VirventSysLogLibrary.SysLogMessage.Severities.Emergency, VirventSysLogLibrary.SysLogMessage.Facilities.log_alert);
            engine = null;
            base.OnStop();
        }
  }   
}
