using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirventPluginContract
{
    public interface IPlugin
    {
        string Name { get; }
        void Run(List<PluginSetting> settings, Message message, out List<PluginMessage> messages);
    }

    public class PluginSetting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class PluginMessage
    {
        public string msg { get; set; }
        public Severities severity { get; set; }
        public Facilities facility { get; set; }
    }

    public class Message
    {
        private const string snortPattern = @"\<(?<hdr>.{1,3})\>(?<dte>.+\:..)\s(?<svr>.+)\s(?<app>.+)\s(?<msg>\[\S+\])\s(?<msgd>.+?)\s\[(?<clas>.+)\]\s\[(?<pri>.+)\]\s\{(?<pro>\S+)\}\s(?<src>\S+)\s..\s(?<des>\S+)";

        // Header
        public DateTimeOffset Received;
        public string Host;
        public IPHostEntry Sender;
        public byte Prival;
        public Facilities Facility;
        public Severities Severity;
        public int Version;
        public string AppName;

        // Message Data
        public string RuleData;
        public string RuleMessage;
        public string Classification;
        public string Priority;
        public string Protocol;
        public string SourceIP;
        public string SourcePort;
        public string DestIP;
        public string DestPort;

        // Things we look at
        private Regex regex = new Regex(snortPattern);

        public Message() : base()
        {

        }

        public Message(string msg) : base()
        {
            string HeaderString;

            GroupCollection groupCollection = regex.Match(msg).Groups;
            HeaderString = groupCollection["hdr"].Value;
            Received = DateTime.Now;
            Prival = byte.Parse(groupCollection["hdr"].Value.Replace("<", "").Replace(">", ""));
            Facility = (Facilities)(Prival / 8);
            Severity = (Severities)(Prival % 8);
            Version = 1;
            Host = groupCollection["svr"].Value;
            Sender = GetSender(Host);
            AppName = groupCollection["app"].Value;

            // build out the message contents
            // Snort rule match
            RuleData = groupCollection["msg"].Value;
            // Snort message
            RuleMessage = groupCollection["msgd"].Value;
            // Classification
            var classification = groupCollection["clas"].Value.Split(":".ToCharArray());
            if (classification.Length == 2)
                Classification = classification[1];
            else
                Classification = groupCollection["clas"].Value;
            // Priority
            var priority = groupCollection["pri"].Value.Split(":".ToCharArray());
            if (priority.Length == 2)
                Priority = priority[1];
            else
                Priority = groupCollection["pri"].Value;
            // Protocol
            Protocol = groupCollection["pro"].Value;

            var src = groupCollection["src"].Value.Split(":".ToCharArray());
            if (src.Length > 1)
            {
                for (int i = 0; i < src.Length-1; i++)
                    SourceIP = src[i]+":";
                SourceIP = SourceIP.Substring(0, SourceIP.Length - 1);
                SourcePort = src[src.Length-1];
            }
            else
            {
                SourceIP = groupCollection["src"].Value;
                SourcePort = "";
            }

            var dst = groupCollection["des"].Value.Split(":".ToCharArray());
            if (dst.Length > 1)
            {
                for (int i = 0; i < dst.Length - 1; i++)
                    DestIP = dst[i] + ":";
                DestIP = DestIP.Substring(0, DestIP.Length - 1);
                DestPort = dst[dst.Length-1];
            }
            else
            {
                DestIP = groupCollection["dst"].Value;
                DestPort = "";
            }
        }

        private IPHostEntry GetSender(string hostName)
        {
            IPHostEntry sender = new IPHostEntry();

            try
            {
                sender = Dns.GetHostEntry(hostName);
            }
            catch
            {
                sender = new IPHostEntry();
                IPAddress sip = new IPAddress(new byte[] { 0, 0, 0, 0 });
                if (IPAddress.TryParse(hostName, out sip))
                {
                    sender.AddressList = new IPAddress[] { sip };
                }
                sender.HostName = hostName;
            }

            return sender;
        }
    }


    public enum Facilities
    {
        kernel_messages,
        user_level_messages,
        mail_system,
        system_daemons,
        security_authorization_messages,
        messages_generated_internally_by_syslogd,
        line_printer_subsystem,
        network_news_subsystem,
        UUCP_subsystem,
        clock_daemon,
        security_authorization_messages1,
        FTP_daemon,
        NTP_subsystem,
        log_audit,
        log_alert,
        clock_daemon_note_2,
        local_use_0__local0,
        local_use_1__local1,
        local_use_2__local2,
        local_use_3__local3,
        local_use_4__local4,
        local_use_5__local5,
        local_use_6__local6,
        local_use_7__local7
    }
    public enum Severities
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
