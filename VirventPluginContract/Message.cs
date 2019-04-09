using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirventPluginContract
{
    public class Message
    {
        // Sample Message: <33>Apr 09 05:06:19 WIN-8HRIVQC9CL0 snort: [1:41819:2] SERVER-APACHE
        // Apache Struts remote code execution attempt [Classification: Attempted Administrator
        // Privilege Gain] [Priority: 1] {TCP} 39.152.208.193:16293 -> 172.31.36.217:80
        private const string snortPattern = @"\<(?<hdr>.{1,3})\>(?<dte>.+\:..)\s(?<svr>.+)\s(?<app>.+)\s(?<msg>\[\S+\])\s(?<msgd>.+?)\s\[(?<clas>.+)\]\s\[(?<pri>.+)\]\s\{(?<pro>\S+)\}\s(?<src>\S+)\s..\s(?<des>\S+)";

        // pattern notes: <hdr>: 33 <dte>: Apr 09 05:06:19 <svr>: WIN-8HRIVQC9CL0 <app>: snort:
        // <msg>: [1:41819:2] <msgd>: SERVER-APACHE Apache Struts remote code execution attempt
        // <clas>: Classification: Attempted Administrator Privilege Gain <pri>: Priority: 1 <pro>:
        // TCP <src>: 39.152.208.193:16293 <des>: 172.31.36.217:80

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

            // build out the message contents Snort rule match
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
                for (int i = 0; i < src.Length - 1; i++)
                    SourceIP = src[i] + ":";
                SourceIP = SourceIP.Substring(0, SourceIP.Length - 1);
                SourcePort = src[src.Length - 1];
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
                DestPort = dst[dst.Length - 1];
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
}