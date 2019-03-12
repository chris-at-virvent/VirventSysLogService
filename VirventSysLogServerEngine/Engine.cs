using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
//using SyslogServer;
using System.Data;
using System.Data.SqlClient;
using VirventSysLogLibrary;
using System.Configuration;

namespace VirventSysLogServerEngine
{
    public class Engine
    {
        public static System.Timers.Timer systemTimer;
        public static readonly DateTime systemCheckTime = new DateTime(1900, 1, 1, 10, 02, 59);

        public TcpListener server;
        public Socket tcpListener;
        public UdpClient udpListener;

        private SqlConnection dataConnection;

        private int processCheckCount;

        // Configuration items
        public int portNumber;
        private LogLevels logLevel;
        private string logSource;
        private string logto;
        public IPAddress listenOn;
        private bool protoTCP;
        private bool protoUDP;
        private string connectionString;
        private string processToCheck;      // name of the child process to check
        private int processCheckInterval;   // how often the timer should tick
        private int processCheckFrequency;  // how often the timer should check

        public Engine()
        {
            processCheckCount = 0;

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

            // set the timer vars
            processToCheck = ConfigurationManager.AppSettings["ProcessToCheck"];
            processCheckInterval = int.Parse(ConfigurationManager.AppSettings["ProcessCheckInterval"]);
            processCheckFrequency = int.Parse(ConfigurationManager.AppSettings["ProcessCheckFrequency"]);


            // Do a check for the configured startup process
            systemTimer = new System.Timers.Timer(processCheckInterval * 1000);
            systemTimer.Elapsed += TimerEvent;
            systemTimer.AutoReset = true;
            systemTimer.Enabled = true;
            systemTimer.Start();

            dataConnection = Data.GetConnection(connectionString);
            if (dataConnection.State != ConnectionState.Open)
            {
                LogToConsole("Database connection is not Open! The state is: " + dataConnection.State.ToString() + "\r\nSending the message to EventLog");
            }

            if (protoUDP)
            {
                udpListener = new UdpClient(new IPEndPoint(listenOn, portNumber));
                udpListener.BeginReceive(new AsyncCallback(ReceiveCallBack), this);
            }
            if (protoTCP)
            {
                tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpListener.Bind(new IPEndPoint(listenOn, portNumber));
                tcpListener.Listen(10);
                tcpListener.BeginAccept(new AsyncCallback(NewConnection), this);
            }

            LogToConsole("Daemon initialized.");

            // loop here somehow so that the tcp listener stays open
            while (true)
            {

            }

        }

        public void NewConnection(IAsyncResult ar)
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
                new AsyncCallback(ReadCallBack), state);
            thisService.LogToConsole("Read call back configured.");
        }

        public void ReceiveCallBack(IAsyncResult ar)
        {
            Engine thisService = (Engine)ar.AsyncState;
            thisService.LogToConsole("New UDP data receiving");
            IPEndPoint groupEP = new IPEndPoint(thisService.listenOn, thisService.portNumber);
            byte[] bytes = thisService.udpListener.Receive(ref groupEP);
            thisService.LogToConsole("UDP Data received: " + bytes.Length.ToString() + "byte from " +
                groupEP.Address.ToString() + ":" + groupEP.Port.ToString());
            thisService.udpListener.BeginReceive(new AsyncCallback(ReceiveCallBack), thisService);
            thisService.LogToConsole("New UDP listener process configured.");
            string rcvd = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            thisService.LogToConsole("UDP received:\r\n" + rcvd);
            thisService.HandleMsg(rcvd, groupEP.Address);
        }

        public void ReadCallBack(IAsyncResult ar)
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
                    new AsyncCallback(ReadCallBack), state);
            }
            else
            {
                if (state.sb.Length > 1)
                {
                    thisService.LogToConsole("TCP: All the data has been read from the client:" +
                        ((System.Net.IPEndPoint)handler.RemoteEndPoint).Address.ToString() + "\r\n" + state.sb.ToString());
                    string rcvd = state.sb.ToString();
                    thisService.HandleMsg(rcvd, ((System.Net.IPEndPoint)handler.RemoteEndPoint).Address);
                }
                handler.Close();
            }

        }

        public void HandleMsg(string rcvd, IPAddress sender)
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
                mymsg.ToSQL(dataConnection);
            }

            LogToConsole("Message handled from " + sender.ToString() + ":\r\n" + rcvd);

        }

        public void LogToConsole(string message)
        {
            if (logLevel == LogLevels.Debug)
                Console.WriteLine(message);

            return;
        }

        public void LogApplicationActivity(string msg, 
            SysLogMessage.Severities severity = SysLogMessage.Severities.Informational,
            SysLogMessage.Facilities facility = SysLogMessage.Facilities.log_audit)
        {
            SysLogMessage message = new SysLogMessage();
            message.received = DateTime.Now;
            message.senderIP = Library.GetLocalAddress().ToString();
            message.sender = Library.GetLocalHost();
            message.severity = SysLogMessage.Severities.Informational;
            message.facility = SysLogMessage.Facilities.log_audit;
            message.version = 1;
            message.hostname = Library.GetLocalHost().HostName;
            message.appName = "Virvent SysLog Service";
            message.procID = "0";
            message.timestamp = DateTime.Now;
            message.msgID = "VIRVENT@32473";

            message.prival = 6;
            message.msg = msg;

            message.ToSQL(dataConnection);
        }

        public void TimerEvent(Object source, ElapsedEventArgs e)
        {
            processCheckCount += 1;

            if (processCheckCount == processCheckFrequency)
            {
                processCheckCount = 0;

                LogToConsole("Checking for " + processToCheck);
                // Check that Snort is running
                var i = ChildProcessChecker.CheckProcess(processToCheck);
                if (i != null)
                {
                    LogToConsole(i.ProcessName + " running as " + i.Id);
                    // Log the result

                    SysLogMessage message = new SysLogMessage();
                    message.received = DateTime.Now;
                    message.senderIP = Library.GetLocalAddress().ToString();
                    message.sender = Library.GetLocalHost();
                    message.severity = SysLogMessage.Severities.Informational;
                    message.facility = SysLogMessage.Facilities.log_audit;
                    message.version = 1;
                    message.hostname = i.MachineName;
                    message.appName = i.ProcessName;
                    message.procID = i.Id.ToString();                                       
                    message.timestamp = DateTime.Now;
                    message.msgID = "VIRVENT@32473";

                    message.prival = 6;
                    message.msg = i.ProcessName + " operational.";

                    message.ToSQL(dataConnection);
                }
                else
                {
                    LogToConsole(processToCheck + " not found!");

                    SysLogMessage message = new SysLogMessage();
                    message.received = DateTime.Now;
                    message.senderIP = Library.GetLocalAddress().ToString();
                    message.sender = Library.GetLocalHost();
                    message.severity = SysLogMessage.Severities.Emergency;
                    message.facility = SysLogMessage.Facilities.kernel_messages;
                    message.version = 1;
                    message.hostname = Library.GetLocalHost().HostName;
                    message.appName = processToCheck;
                    message.procID = "0";
                    message.timestamp = DateTime.Now;
                    message.msgID = "VIRVENT@32473";

                    message.prival = 6;
                    message.msg = processToCheck + " not loaded.";

                    message.ToSQL(dataConnection);
                }

            }
        }

    }

    // IETF RFC 5424 - PRI
    public enum LogLevels
    {
        Emergency,
        Alert,
        Critical,
        Error,
        Warning,
        Notice,
        Informational,
        Debug
    }
}
