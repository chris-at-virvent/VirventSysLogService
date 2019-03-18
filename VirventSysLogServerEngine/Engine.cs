using Nerdle.AutoConfig;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using VirventPluginContract;
using VirventSysLogServerEngine.Configuration;
using VirventSysLogServerEngine.Extensions;
using VirventSysLogServerEngine.Helpers;
using VirventSysLogServerEngine.ThreadHelpers;

namespace VirventSysLogServerEngine
{
    /// <summary>
    ///   <para>Main thread that the Virvent Syslog Service calls to run all monitoring and logging</para>
    /// </summary>
    public class Engine
    {
        public static System.Timers.Timer systemTimer;
        public static readonly DateTime systemCheckTime = new DateTime(1900, 1, 1, 10, 02, 59);

        private double timerLatency = 0;
        private DateTime lastTimerEvent;

        public TcpListener server;
        public Socket tcpListener;
        public UdpClient udpListener;

        public SqlConnection dataConnection;

        public Dictionary<string, IPlugin> PluginDictionary;
        public List<Plugin> Plugins;


        // Configuration items
        public int portNumber;
        public LogLevels logLevel;
        public string logSource;
        public string logto;
        public IPAddress listenOn;
        private bool protoTCP;
        private bool protoUDP;
        public string connectionString;

        private string PluginDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class.
        /// and processes the startup routine
        /// </summary>
        public Engine(
            bool StartEngine = true,
            bool StartTimer = true
            )
        {
            // load configuration file
            portNumber = int.Parse(ConfigurationManager.AppSettings["PortNumber"]);
            logLevel = (LogLevels)int.Parse(ConfigurationManager.AppSettings["LogLevel"]);
            logSource = ConfigurationManager.AppSettings["LogSource"];
            logto = ConfigurationManager.AppSettings["LogName"];

            listenOn = IPAddress.Any;
            IPAddress.TryParse(ConfigurationManager.AppSettings["IPAddressToListen"], out listenOn);
            protoTCP = ((ConfigurationManager.AppSettings["Protocol"]).ToLower().Contains("tcp"));
            protoUDP = ((ConfigurationManager.AppSettings["Protocol"]).ToLower().Contains("udp"));

            connectionString = ConfigurationManager.ConnectionStrings["SysLogConnString"].ConnectionString;

            PluginDirectory = ConfigurationManager.AppSettings["PluginDirectory"];
            if (PluginDirectory == "")
                PluginDirectory = Environment.CurrentDirectory + "\\plugins";
            else if (!PluginDirectory.Contains(":"))
                PluginDirectory = Environment.CurrentDirectory + PluginDirectory;


            LogToConsole("Initalizing Virvent Syslog Service");
            LogToConsole("Plugin Directory: " + PluginDirectory);
            // Do a check for the configured startup process

            // load plugin configurations
            LogToConsole("Loading plugin configurations");
            var pluginsConfig = AutoConfig.Map<IPluginConfiguration>();

            // load plugins - pass the config file
            LogToConsole("Loading plugins");
            Plugins = new List<Plugin>();
            PluginDictionary = new Dictionary<string, IPlugin>();
            ICollection<IPlugin> plugins = PluginManager.LoadPlugins(PluginDirectory);
            if (plugins.Count > 0)
                LogToConsole("Found " + plugins.Count + " plugins.");


            LogToConsole("Binding configurations to plugins.");


            // assemble the plugin with it's configuration
            foreach (var config in pluginsConfig.PluginSettings)
            {
                // find the assembly
                foreach (var i in plugins)
                {
                    if (i.Name == config.Name)
                    {
                        try {
                            var thisPlugin = new Plugin()
                            {
                                Name = config.Name,
                                Hours = config.Hours,
                                Minutes = config.Minutes,
                                Seconds = config.Seconds,
                                TimeUntilEvent = (config.Hours * 60 * 60) + (config.Minutes * 60) + (config.Seconds),
                                SecondsSinceLastEvent = 0,
                                PluginAssembly = i,
                                Settings = new List<PluginSetting>()
                            };

                            foreach (var setting in config.Settings)
                            {
                                thisPlugin.Settings.Add(new PluginSetting() { Key = setting.Key, Value = setting.Value });
                            }
                            Plugins.Add(thisPlugin);

                            LogToConsole("PLUGIN MANAGER: Loaded " + thisPlugin.Name + " successfully.\r\nTimeUntilEvent: " +thisPlugin.TimeUntilEvent);
                        }
                        catch (Exception ex)
                        {
                            LogToConsole("PLUGIN MANAGER: Error loading plugin: " + i.Name + "\r\n" + ex.Message + "\r\n" + ex.StackTrace);
                        }
                    }
                }
            }


            LogToConsole("Daemon initialized - starting engine.");
            // LogApplicationActivity("Virvent Syslog Server Initialized", SysLogMessage.Severities.Informational, SysLogMessage.Facilities.log_audit);

            if (StartEngine || StartTimer)
                Start(StartEngine, StartTimer);

        }

        public void Start(
            bool StartEngine = true,
            bool StartTimer = true)
        {
            if (StartTimer)
            {
                // start the timer on it's own thread
                systemTimer = new System.Timers.Timer(1000);
                systemTimer.Elapsed += ProcessCheckEvent;
                systemTimer.AutoReset = true;
                systemTimer.Enabled = true;
                systemTimer.Start();
                LogToConsole("Process checker started");
            }

            if (StartEngine)
            {

                LogToConsole("Connecting to database server:\r\n" + connectionString);
                dataConnection = Data.GetConnection(connectionString);
                if (dataConnection.State != ConnectionState.Open)
                {
                    LogToConsole("Database connection is not Open! The state is: " + dataConnection.State.ToString() + "\r\nSending the message to EventLog");
                }

                if (protoUDP)
                {
                    udpListener = new UdpClient(new IPEndPoint(listenOn, portNumber));
                    udpListener.BeginReceive(new AsyncCallback(UDPCallback), this);
                    LogToConsole("UDP Listener open");

                }
                if (protoTCP)
                {
                    tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    tcpListener.Bind(new IPEndPoint(listenOn, portNumber));
                    tcpListener.Listen(10);
                    tcpListener.BeginAccept(new AsyncCallback(TCPConnection), this);
                    LogToConsole("TCP Listener Open");

                }
            }

            LogToConsole("Engine Initialized.");

        }

        /// <summary>
        /// Handles new TCP connection.
        /// </summary>
        /// <param name="ar">IAsyncResult</param>
        public void TCPConnection(IAsyncResult ar)
        {
            Engine thisService = (Engine)ar.AsyncState;
            thisService.LogToConsole("New TCP connection initiated.");
            Socket lsnr = thisService.tcpListener;
            Socket handler = lsnr.EndAccept(ar);
            StateObject state = new StateObject
            {
                WorkSocket = thisService
            };
            handler.BeginReceive(
                state.buffer,
                0,
                StateObject.BufferSize,
                0,
                new AsyncCallback(TCPCallback), state);
            thisService.LogToConsole("Read call back configured.");
        }

        /// <summary>  UDP Callback handler</summary>
        /// <param name="ar">IAsyncResult</param>
        public void UDPCallback(IAsyncResult ar)
        {
            Engine thisService = (Engine)ar.AsyncState;
            thisService.LogToConsole("New UDP data receiving");
            IPEndPoint groupEP = new IPEndPoint(thisService.listenOn, thisService.portNumber);
            byte[] bytes = thisService.udpListener.Receive(ref groupEP);
            thisService.LogToConsole("UDP Data received: " + bytes.Length.ToString() + "byte from " +
                groupEP.Address.ToString() + ":" + groupEP.Port.ToString());
            thisService.udpListener.BeginReceive(new AsyncCallback(UDPCallback), thisService);
            thisService.LogToConsole("New UDP listener process configured.");
            string rcvd = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            thisService.LogToConsole("UDP received:\r\n" + rcvd);
            thisService.LogToDatabase(rcvd, groupEP.Address);
        }

        /// <summary>  TCP Callback handler</summary>
        /// <param name="ar">IAsyncResult</param>
        public void TCPCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Engine thisService = (Engine)state.WorkSocket;
            thisService.LogToConsole("TCP data receive starting.");
            Socket handler = thisService.tcpListener;
            int read = handler.EndReceive(ar);
            if (read > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(TCPCallback), state);
            }
            else
            {
                if (state.sb.Length > 1)
                {
                    thisService.LogToConsole("TCP: All the data has been read from the client:" +
                        ((System.Net.IPEndPoint)handler.RemoteEndPoint).Address.ToString() + "\r\n" + state.sb.ToString());
                    string rcvd = state.sb.ToString();
                    thisService.LogToDatabase(rcvd, ((System.Net.IPEndPoint)handler.RemoteEndPoint).Address);
                }
                handler.Close();
            }

        }

        /// <summary>Logs the Syslog message to database.</summary>
        /// <param name="rcvd">  Message that was received from incoming transmission</param>
        /// <param name="sender">The sender IPAddress</param>
        public void LogToDatabase(string rcvd, IPAddress sender)
        {
            if (logLevel != LogLevels.Debug)
            {
                LogToConsole("Start handling received data from " + sender.ToString() + "\r\n" + rcvd);
                SysLogMessage mymsg = new SysLogMessage(rcvd);
                mymsg.Sender = sender.ToString();
                LogToConsole("SyslogMessage object created.");

                dataConnection = Data.GetConnection(connectionString);

                if (dataConnection.State != ConnectionState.Open)
                {
                    LogToConsole("Database connection is not Open! The state is: " + dataConnection.State.ToString() + "\r\nSending the message to EventLog");
                }
                else
                {
                    Data.GenerateEntry(dataConnection, mymsg);
                }
            }
            LogToConsole("Message handled from " + sender.ToString() + ":\r\n" + rcvd);

        }

        /// <summary>
        ///   <para>
        ///  Logs to console or event log.</para>
        ///   <para>If running in debug mode - will log the output to the console.<br />If running in production mode - will log the output to the Windows Event Log.</para>
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogToConsole(string message)
        {
            if (logLevel == LogLevels.Debug)
                Console.WriteLine(message);
            else
                EventLog.WriteEntry(this.logSource, message, EventLogEntryType.Information);
            return;
        }

        public void LogApplicationActivity(string msg,
            Severities severity,
            Facilities facility,
            SqlConnection dataConnection)
        {
            SysLogMessage message = new SysLogMessage();
            message.received = DateTime.Now;
            message.senderIP = IPHelpers.GetLocalAddress().ToString();
            message.sender = IPHelpers.GetLocalHost();
            message.severity = Severities.Informational;
            message.facility = Facilities.log_audit;
            message.version = 1;
            message.hostname = IPHelpers.GetLocalHost().HostName;
            message.appName = "Virvent SysLog Service";
            message.procID = "0";
            message.timestamp = DateTime.Now;
            message.msgID = "VIRVENT@32473";

            message.prival = 6;
            message.msg = msg;

            Data.GenerateEntry(dataConnection, message);
        }

        /// <summary>  Handles the ProcessCheck timer events</summary>
        /// <param name="source">The source.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        public void ProcessCheckEvent(Object source, ElapsedEventArgs e)
        {
            int countersToFix = 0;

            if (lastTimerEvent == new DateTime())
            {
                lastTimerEvent = DateTime.Now;
            }
            else
            {
                double currentLatency = DateTime.Now.Subtract(lastTimerEvent).TotalMilliseconds;
                timerLatency += currentLatency;

                //LogToConsole(currentLatency.ToString());
                if (timerLatency > 10000)
                {
                    var newcounters = (timerLatency - (timerLatency % 10000))/10000;
                    countersToFix = int.Parse(newcounters.ToString());
                    //LogToConsole("Timer: Adjusting clock by " + countersToFix + " ticks for latency of " + timerLatency);
                    timerLatency = timerLatency - (10000*countersToFix);
                }

                lastTimerEvent = DateTime.Now;
            }

            //LogToConsole("Timer triggered : " + DateTime.Now.ToRfc3339String());
            // check each plugin for settings
            foreach (var plugin in Plugins)
            {
                if (countersToFix > 0)
                    plugin.SecondsSinceLastEvent += countersToFix;

                if (plugin.SecondsSinceLastEvent > plugin.TimeUntilEvent)
                {
                    PluginMessage pluginMessage = new PluginMessage();
                    var thisProc = new ProcessPluginThread(plugin, this);

                    Thread thisThread = new Thread(
                        new ThreadStart(
                            thisProc.Process
                        )
                    );

                    thisThread.Start();
                    LogToConsole(" - Processing plugin event for :" + plugin.Name);
                    //thisThread.Join();
                    plugin.SecondsSinceLastEvent = 0;
                }
                else
                {
                    plugin.SecondsSinceLastEvent += 1;
                }
            }

            return;
        }

    }

    //public class ProcessThread
    //{
    //    private Plugin Plugin;
    //    public List<PluginMessage> PluginMessages;
    //    private ProcessThreadCallback ProcessThreadCallback;
    //    public ProcessThread(Plugin plugin, ProcessThreadCallback processThreadCallback)
    //    {
    //        Plugin = plugin;
    //        PluginMessages = new List<PluginMessage>();
    //        ProcessThreadCallback = processThreadCallback;
    //    }

    //    public void Process()
    //    {
    //        Plugin.PluginAssembly.Run(Plugin.Settings, out PluginMessages);
    //        if (PluginMessages.Count != 0)
    //            ProcessThreadCallback(PluginMessages);
    //    }

    //}

    //public delegate void ProcessThreadCallback(List<PluginMessage> messages);


}



//processCheckCount += 1;

//if (processCheckCount == processCheckFrequency)
//{
//    processCheckCount = 0;

//    LogToConsole("Checking processes");
//    // Check that Snort is running
//    var checkedProcesses = ChildProcessChecker.CheckProcess();

//    LogToConsole("Found " + checkedProcesses.Count + " to validate");

//    if (checkedProcesses != null)
//    {
//        foreach (var i in checkedProcesses)
//        {
//            if (i.ProcessIsRunning)
//            {
//                LogToConsole(i.Process.ProcessName + " running as " + i.Process.Id);
//                // Log the result

//                SysLogMessage message = new SysLogMessage();
//                message.received = DateTime.Now;
//                message.senderIP = Library.GetLocalAddress().ToString();
//                message.sender = Library.GetLocalHost();
//                message.severity = SysLogMessage.Severities.Informational;
//                message.facility = SysLogMessage.Facilities.log_audit;
//                message.version = 1;
//                message.hostname = i.Process.MachineName;
//                message.appName = i.Process.ProcessName;
//                message.procID = i.Process.Id.ToString();
//                message.timestamp = DateTime.Now;
//                message.msgID = "VIRVENT@32473";

//                message.prival = 6;
//                message.msg = i.Process.ProcessName + " operational.";

//                Data.GenerateEntry(dataConnection, message);
//            }
//            else
//            {
//                LogToConsole(i.ProcessToCheck + " not operational");

//                SysLogMessage message = new SysLogMessage();
//                message.received = DateTime.Now;
//                message.senderIP = Library.GetLocalAddress().ToString();
//                message.sender = Library.GetLocalHost();
//                message.severity = SysLogMessage.Severities.Emergency;
//                message.facility = SysLogMessage.Facilities.kernel_messages;
//                message.version = 1;
//                message.hostname = Library.GetLocalHost().HostName;
//                message.appName = i.ProcessToCheck;
//                message.procID = "0";
//                message.timestamp = DateTime.Now;
//                message.msgID = "VIRVENT@32473";

//                message.prival = 6;
//                message.msg = i.ProcessToCheck + " not loaded.";

//                Data.GenerateEntry(dataConnection, message);
//            }
//        }
//    }
//    else
//    {
//        LogToConsole("No processes to check. Engine Disabled.");
//    }

//}