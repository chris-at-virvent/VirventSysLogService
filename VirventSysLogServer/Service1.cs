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
            base.OnStart(args);
        }

        protected override void OnContinue()
        {
            Engine engine = new Engine();
            base.OnContinue();
        }

        protected override void OnPause()
        {
            engine = null;
            base.OnPause();
        }

        protected override void OnStop()
        {
            engine = null;
            base.OnStop();
        }
  }   
}
