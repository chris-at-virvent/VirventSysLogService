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

    public partial class VirventSysLogService : ServiceBase
    {

        public Engine engine = null;

        public VirventSysLogService()
        {
            InitializeComponent();            
        }
        
        protected override void OnStart(string[] args)
        {
            engine = new Engine();            
            engine.LogApplicationActivity("Service Started Successfully");
            base.OnStart(args);
        }

        protected override void OnContinue()
        {
            engine = new Engine();
            engine.LogApplicationActivity("Service Resumed Successfully");
            base.OnContinue();
        }

        protected override void OnPause()
        {
            engine.LogToConsole("Stopping Service");
            engine.LogApplicationActivity("Service Paused - Logging Is Disabled", VirventSysLogLibrary.SysLogMessage.Severities.Warning, VirventSysLogLibrary.SysLogMessage.Facilities.log_alert);
            engine = null;
            base.OnPause();
        }

        protected override void OnStop()
        {
            engine.LogToConsole("Stopping Service");
            engine.LogApplicationActivity("Service Stopped - Logging Is Disabled", VirventSysLogLibrary.SysLogMessage.Severities.Emergency, VirventSysLogLibrary.SysLogMessage.Facilities.log_alert);
            engine = null;
            base.OnStop();
        }

        #region "Internals"

        #endregion

        private void bgEngineThread_DoWork(object sender, DoWorkEventArgs e)
        {

        }
    }
}
