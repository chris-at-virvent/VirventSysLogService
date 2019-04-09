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
using VirventDataContract;
using VirventPluginContract;
using VirventSysLogServerEngine.Configuration;
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
        public WhiteListHelper whiteList;

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
        private Severities WhiteListSeverity;

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class.
        /// and processes the startup routine
        /// </summary>
        public Engine(
            bool StartEngine = true,
            bool StartTimer = true
            )
        {
            Console.WriteLine("Loading Configuration File");
            // load configuration file
            portNumber = int.Parse(ConfigurationManager.AppSettings["PortNumber"]);
            logLevel = (LogLevels)int.Parse(ConfigurationManager.AppSettings["LogLevel"]);
            logSource = ConfigurationManager.AppSettings["LogSource"];
            logto = ConfigurationManager.AppSettings["LogName"];

            Console.WriteLine("Loading Listener Data");
            listenOn = IPAddress.Any;
            IPAddress.TryParse(ConfigurationManager.AppSettings["IPAddressToListen"], out listenOn);
            protoTCP = ((ConfigurationManager.AppSettings["Protocol"]).ToLower().Contains("tcp"));
            protoUDP = ((ConfigurationManager.AppSettings["Protocol"]).ToLower().Contains("udp"));

            Console.WriteLine("Setting Connnection String");
            connectionString = ConfigurationManager.ConnectionStrings["SysLogConnString"].ConnectionString;

            Console.WriteLine("Loading Plugin Configurations");
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
                        try
                        {
                            var thisPlugin = new Plugin()
                            {
                                Name = config.Name,
                                Hours = config.Hours,
                                Minutes = config.Minutes,
                                Seconds = config.Seconds,
                                AfterStartup = config.AfterStartup,
                                OnMessageEvent = config.OnMessageEvent,
                                TimeUntilEvent = (config.Hours * 60 * 60) + (config.Minutes * 60) + (config.Seconds),
                                SecondsSinceLastEvent = 0,
                                ConnectionString = connectionString,
                                PluginAssembly = i,
                                Settings = new List<PluginSetting>()
                            };

                            foreach (var setting in config.Settings)
                            {
                                thisPlugin.Settings.Add(new PluginSetting() { Key = setting.Key, Value = setting.Value });
                            }
                            thisPlugin.Settings.Add(new PluginSetting() { Key = "SysLogConnString", Value = connectionString });
                            Plugins.Add(thisPlugin);

                            LogToConsole("PLUGIN MANAGER: Loaded " + thisPlugin.Name + " successfully.\r\nTimeUntilEvent: " + thisPlugin.TimeUntilEvent);
                        }
                        catch (Exception ex)
                        {
                            LogToConsole("PLUGIN MANAGER: Error loading plugin: " + i.Name + "\r\n" + ex.Message + "\r\n" + ex.StackTrace);
                        }
                    }
                }
            }

            LogToConsole("Getting WhiteList Entries");
            whiteList = new WhiteListHelper(ConfigurationManager.AppSettings["WhitelistDirectory"]);
            WhiteListSeverity = (Severities)int.Parse(ConfigurationManager.AppSettings["WhitelistPolicy"]);

            LogToConsole("Server initialized - starting engine.");

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
                LogToConsole("Plugin engine started");
            }

            if (StartEngine)
            {

                LogToConsole("Connecting to database server:\r\n" + connectionString);
                dataConnection = Data.GetConnection(connectionString);
                if (dataConnection.State != ConnectionState.Open)
                {
                    LogToConsole("Database connection is not Open! The state is: " + dataConnection.State.ToString() + "\r\nSending the message to EventLog");
                }
                else
                {
                    LogToConsole("Database server connected.");
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

            LogToConsole("Engine Started.");
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
            Message msg = new Message(rcvd);

            if (whiteList.IsWhiteListed(msg.SourceIP))
            {
                msg.Severity = WhiteListSeverity;
                msg.RuleMessage = "WHITELISTED: " + msg.RuleMessage;
            }
            foreach (var plugin in Plugins)
            {
                if (plugin.OnMessageEvent == 1)
                {
                    ExecutePlugin(plugin, new Message(rcvd));
                }
            }

            thisService.LogToDatabase(msg, groupEP.Address);
           
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
                    Message msg = new Message(rcvd);

                    if (whiteList.IsWhiteListed(msg.SourceIP))
                    {
                        msg.Severity = WhiteListSeverity;
                        msg.RuleMessage = "WHITELISTED: " + msg.RuleMessage;
                    }
                    foreach (var plugin in Plugins)
                    {
                        if (plugin.OnMessageEvent == 1)
                        {
                            ExecutePlugin(plugin, new Message(rcvd));
                        }
                    }

                    thisService.LogToDatabase(msg, ((System.Net.IPEndPoint)handler.RemoteEndPoint).Address);

                }
                handler.Close();
            }
        }

        /// <summary>Logs the Syslog message to database.</summary>
        /// <param name="rcvd">  Message that was received from incoming transmission</param>
        /// <param name="sender">The sender IPAddress</param>
        public void LogToDatabase(Message rcvd, IPAddress sender)
        {
            //if (logLevel != LogLevels.Debug)
            //{
            LogToConsole("Start handling received data from " + sender.ToString());
            //SysLogMessage mymsg = new SysLogMessage(rcvd);
            //message.Sender = sender.ToString();
            LogToConsole("SyslogMessage object created.");

            dataConnection = Data.GetConnection(connectionString);

            if (dataConnection.State != ConnectionState.Open)
            {
                LogToConsole("Database connection is not Open! The state is: " + dataConnection.State.ToString() + "\r\nSending the message to EventLog");
            }
            else
            {
                Data.GenerateEntry(dataConnection, rcvd);
            }

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
            Message message = new Message();
            message.Received = DateTime.Now;
            message.Sender = IPHelpers.GetLocalHost();
            message.Severity = severity;
            message.Facility = facility;
            message.Version = 1;
            message.AppName = "Virvent SysLog Service";

            message.RuleData = "Syslog Message";
            message.RuleMessage = msg;
            message.Classification = "Notification";
            message.Priority = "9";
            message.SourceIP = IPHelpers.GetLocalAddress().ToString();
            message.SourcePort = "";
            message.DestIP = "";
            message.DestPort = "";

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
                    var newcounters = (timerLatency - (timerLatency % 10000)) / 10000;
                    countersToFix = int.Parse(newcounters.ToString());
                    //LogToConsole("Timer: Adjusting clock by " + countersToFix + " ticks for latency of " + timerLatency);
                    timerLatency = timerLatency - (10000 * countersToFix);
                }

                lastTimerEvent = DateTime.Now;
            }

            //LogToConsole("Timer triggered : " + DateTime.Now.ToRfc3339String());
            // check each plugin for settings
            foreach (var plugin in Plugins)
            {
                if (plugin.OnMessageEvent == 1)
                    break;

                if (plugin.AfterStartup > 0)
                {
                    plugin.SecondsSinceLastEvent = plugin.TimeUntilEvent - plugin.AfterStartup + 1;
                    plugin.AfterStartup = 0;
                }

                if (countersToFix > 0)
                    plugin.SecondsSinceLastEvent += countersToFix;

                if (plugin.SecondsSinceLastEvent > plugin.TimeUntilEvent)
                {
                    ExecutePlugin(plugin, new Message());
                    plugin.SecondsSinceLastEvent = 0;
                }
                else
                {
                    plugin.SecondsSinceLastEvent += 1;
                }
            }

            return;
        }

        private void ExecutePlugin(Plugin plugin, Message message)
        {
            PluginMessage pluginMessage = new PluginMessage();
            var thisProc = new ProcessPluginThread(plugin, this, message);

            Thread thisThread = new Thread(
                new ThreadStart(
                    thisProc.Process
                )
            );

            thisThread.Start();
            LogToConsole(" - Processing plugin event for :" + plugin.Name);
        }

    }
}