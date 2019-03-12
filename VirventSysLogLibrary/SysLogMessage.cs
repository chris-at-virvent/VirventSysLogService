using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirventSysLogLibrary
{
    public class SysLogMessage
    {
        private const string monthspattern = @"(Jan)|(Feb)|(Mar)|(Apr)|(May)|(Ju[nl])|(Aug)|(Sep)|(Oct)|(Nov)|(Dec)";
        private const string timestemppattern = @"(?:\-|(?<ed>\d\d\d\d\-\d\d\-\d\dT\d\d\:\d\d\:\d\d(?:\.\d{1,6})?(?:Z|(?:[\+\-]\d\d\:\d\d))))";
        private const string headerpattern = @"^(?<head>\<(?<pri>\d{1,3})\>(?<ver>\d{0,3})\s(?<ts>" + timestemppattern + @")\s(?<hn>\S+)\s(?<an>\S+)\s(?<pr>\S+)\s(?<msgid>\S+))";
        private const string sdpattern = @"(?<sd>\-|(?:\[\S+(?:\s\S+\=\x22[^\x22]+\x22)*\])+)";
        private const string fullPattern = headerpattern + @"\s" + sdpattern + @"\s?(?<msg>.*)$";
        private const string bSDPattern = @"^\<(?<pri>\d{1,3})\>(?<bsdts>(" + monthspattern + @")\s[\s123]\d\s\d\d\:\d\d\:\d\d)\s(?<hn>\S+)\s(?<msg>(?<tag>\w*)(?<cnt>.*))$";

        private Regex formatregexp = new Regex(fullPattern);
        private Regex bSDRE = new Regex(bSDPattern);

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
        public enum Monthes { Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec }
        public static readonly string[] SeveritiesLong = new string[]
        {
              "system is unusable",
              "action must be taken immediately",
              "critical conditions",
              "error conditions",
              "warning conditions",
              "normal but significant condition",
              "informational messages",
              "debug-level messages"
        };

        public DateTimeOffset received;
        public IPHostEntry sender;
        public string senderIP;
        public byte prival;               // from 0 to 191 
        public Facilities facility;
        public Severities severity;
        public Int16 version;             // from 1 to 999
        public string hostname;           // 1-255 char 
        public DateTimeOffset timestamp;  // yyyy-mm-ddThh:mm:ss[.xxxxxx]Z[+,-]dh:dm
        public string appName;            // 1-48 char
        public string procID;             // 1-128 char
        public string msgID;              // 1-32 char
        public string[] sdID;             // 1-32 char data name
        public Hashtable sdParams;
        public string msg;                // Message string

        public string Header
        {
            get
            {
                string ts = "-";
                if (timestamp != null) { ts = timestamp.ToString("O"); };
                return received.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.FFF\ zzz") + ":<" +
                    prival.ToString().Trim() + ">" + version.ToString().Trim() + " " +
                    ts + " " + hostname + " " + appName + " " + procID + " " + msgID;
            }
            set
            {
                received = DateTimeOffset.Now;
                string[] hdrstrs = value.Split(" ".ToCharArray(), StringSplitOptions.None);
                prival = byte.Parse(hdrstrs[0].Substring(1).Split(">".ToCharArray())[0]);
                facility = (Facilities)(prival / 8);
                severity = (Severities)(prival % 8);
                version = Int16.Parse(hdrstrs[0].Substring(1).Split(">".ToCharArray())[1]);
                string dto = hdrstrs[1];
                string fmt = @"yyyy\-MM\-dd\THH\:mm\:ss";
                if (dto == "-")
                {
                    timestamp = new DateTimeOffset();
                }
                else
                {
                    if (dto.IndexOf("Z") > -1) dto = dto.Substring(0, dto.Length - 1) + "+00:00";
                    if (dto.IndexOf(".") > -1)
                    {
                        int nof = dto.Length - dto.IndexOf(".") - 7;
                        fmt += ".";
                        for (int i = 0; i < nof; i++) fmt += "F";
                    }
                    fmt += "zzz";
                    timestamp = DateTimeOffset.ParseExact(dto, fmt, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (timestamp < DateTimeOffset.Parse("1900.01.01")) { timestamp = DateTimeOffset.Parse("1900.01.01"); }
                hostname = hdrstrs[2];
                appName = hdrstrs[3];
                procID = hdrstrs[4];
                msgID = hdrstrs[5];
            }
        }
        public string SD
        {
            get
            {
                string s = "";
                foreach (string sdid in sdID)
                {
                    if (sdid != "")
                    {
                        s += "[" + sdid;
                        foreach (DictionaryEntry de in sdParams)
                        {
                            if (((string[])de.Key)[0] == sdid)
                            {
                                s += " " + ((string[])de.Key)[1] + "=\"" + (string)de.Value + "\"";
                            }
                        }
                        s += "]";
                    }
                }
                if (s == "") { s = "-"; }
                return s;
            }
            set
            {
                if (value == "-")
                {
                    sdID = new string[1] { "" };
                    sdParams = new System.Collections.Hashtable();
                }
                else
                {
                    string[] sa = value.Split("[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    sdParams = new Hashtable();
                    sdID = new string[sa.Length];
                    for (int j = 0; j < sa.Length; j++)
                    {
                        string s = sa[j];
                        sdID[j] = s.Split(" ".ToCharArray())[0];
                        s = s.Substring(sdID[j].Length + 1);
                        string[] p = s.Split("=\"".ToCharArray());
                        for (int i = 0; i < p.Length - 1; i += 3)
                        {
                            sdParams.Add(new string[2] { sdID[j], p[i].Trim() }, p[i + 2]);
                        }
                    }
                }
            }
        }
        public string Msg
        {
            get
            {
                return msg;
            }
            set
            {
                if (value.Length > 3)
                {
                    if (UTF8Encoding.UTF8.GetString(new byte[] { 239, 187, 191 }) == value.Substring(0, 3))
                    {
                        msg = value.Substring(3);
                    }
                    else
                    {
                        msg = value;
                    }
                }
                else
                    msg = value;
            }
        }
        public string Sender              // Set as an IP addresse, get as hostname : ip
        {
            get
            {
                return sender.HostName + " : " + senderIP;
            }
            set
            {
                try
                {
                    senderIP = value;
                    sender = Dns.GetHostEntry(value);
                }
                catch
                {
                    sender = new IPHostEntry();
                    IPAddress sip = new IPAddress(new byte[] { 0, 0, 0, 0 });
                    if (IPAddress.TryParse(value, out sip))
                    {
                        sender.AddressList = new IPAddress[] { sip };
                    }
                    sender.HostName = value;
                }
            }
        }

        public static bool IsStructuredMessage(string msg)
        {
            Regex myre = new Regex(fullPattern);
            Regex mybsd = new Regex(bSDPattern);
            return myre.IsMatch(msg) | mybsd.IsMatch(msg);
        }

        public SysLogMessage()
        {
            //Header = "";
            SD = "-";
            //msg = "";
        }

        public SysLogMessage(string hdr, string sd, string msg)
        {
            Header = hdr;
            SD = sd;
            Msg = msg;
        }

        public SysLogMessage(string msg)
        {
            string headerstr;
            string structuredDatastr;
            string messagestr;
            if (formatregexp.IsMatch(msg))
            {
                GroupCollection mg = formatregexp.Match(msg).Groups;
                headerstr = mg["head"].Value;
                structuredDatastr = mg["sd"].Value;
                messagestr = mg["msg"].Success ? mg["msg"].Value : "";
                received = DateTimeOffset.Now;
                prival = byte.Parse(mg["pri"].Value);
                facility = (Facilities)(prival / 8);
                severity = (Severities)(prival % 8);
                version = short.Parse("0" + mg["ver"].Value);
                if (mg["ed"].Success)
                {
                    string dto = mg["ed"].Value;
                    string fmt = @"yyyy\-MM\-dd\THH\:mm\:ss";
                    if (dto.IndexOf("Z") > -1) dto = dto.Substring(0, dto.Length - 1) + "+00:00";
                    if (dto.IndexOf(".") > -1)
                    {
                        int nof = dto.Length - dto.IndexOf(".") - 7;
                        fmt += ".";
                        for (int i = 0; i < nof; i++) fmt += "F";
                    }
                    fmt += "zzz";
                    timestamp = DateTimeOffset.ParseExact(dto, fmt, System.Globalization.CultureInfo.InvariantCulture);
                    if (timestamp < DateTimeOffset.Parse("1900.01.01")) { timestamp = DateTimeOffset.Parse("1900.01.01"); }
                }
                else if (mg["ld"].Success)
                {
                    string dto = received.Year.ToString() + " " + mg["ld"].Value;
                    string fmt = @"yyyy\ MMM\ d\ H\:m\:s";
                    timestamp = DateTimeOffset.ParseExact(dto, fmt, System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                    timestamp = DateTimeOffset.Now;
                hostname = mg["hn"].Value;
                appName = mg["an"].Value;
                procID = mg["pr"].Value;
                msgID = mg["msgid"].Value;

                SD = structuredDatastr;
                Msg = messagestr;
            }
            else if (bSDRE.IsMatch(msg))
            {
                GroupCollection mg = bSDRE.Match(msg).Groups;
                Msg = mg["cnt"].Success ? mg["cnt"].Value : "";
                SD = "-";
                prival = byte.Parse(mg["pri"].Success ? mg["pri"].Value : "13");
                facility = (Facilities)(prival / 8);
                severity = (Severities)(prival % 8);
                version = 0;
                Sender = mg["hn"].Value;
                hostname = mg["hn"].Value;
                appName = mg["tag"].Success ? (mg["tag"].Value == "" ? "BSD" : mg["tag"].Value) : "BSD";
                procID = "0";
                msgID = "BSD";
                timestamp = DateTimeOffset.ParseExact(DateTimeOffset.Now.Year.ToString() + " " + mg["bsdts"].Value.Replace("  ", " "), "yyyy MMM d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                received = DateTimeOffset.Now;
            }
            else
            {
                Header = "<13>1 - - - - -";
                SD = "-";
                Msg = msg;
            }
        }

        public void ToSQL(SqlConnection sqlconnect)
        {
            if (sqlconnect.State == ConnectionState.Open)
            {
                SqlCommand sqlCommand = new SqlCommand("", sqlconnect);
                Int64 thissdid = 0;
                if (SD != "-")
                {
                    foreach (DictionaryEntry de in sdParams)
                    {
                        string tsdid = ((string[])de.Key)[0];
                        string tsdpn = ((string[])de.Key)[1];
                        string tsdpv = (string)de.Value;
                        if (thissdid == 0)
                        {
                            sqlCommand.CommandText = "NewSD";
                            sqlCommand.CommandType = CommandType.StoredProcedure;
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue("@sdid", tsdid);
                            sqlCommand.Parameters.AddWithValue("@sdpn", tsdpn);
                            sqlCommand.Parameters.AddWithValue("@sdpv", tsdpv);
                            thissdid = (Int64)sqlCommand.ExecuteScalar();
                        }
                        else
                        {
                            sqlCommand.CommandText = "NextSD";
                            sqlCommand.CommandType = CommandType.StoredProcedure;
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue("@nsdid", thissdid);
                            sqlCommand.Parameters.AddWithValue("@sdid", tsdid);
                            sqlCommand.Parameters.AddWithValue("@sdpn", tsdpn);
                            sqlCommand.Parameters.AddWithValue("@sdpv", tsdpv);
                            sqlCommand.ExecuteScalar();
                        }
                    }
                }
                sqlCommand.CommandText = "NewLog";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Clear();
                sqlCommand.Parameters.AddWithValue("@timestamp", received.UtcDateTime);
                sqlCommand.Parameters.AddWithValue("@sourceip", senderIP);
                sqlCommand.Parameters.AddWithValue("@sourcename", sender.HostName);
                sqlCommand.Parameters.AddWithValue("@severity", severity);
                sqlCommand.Parameters.AddWithValue("@facility", facility);
                sqlCommand.Parameters.AddWithValue("@version", version);
                sqlCommand.Parameters.AddWithValue("@hostname", hostname);
                sqlCommand.Parameters.AddWithValue("@appname", appName);
                sqlCommand.Parameters.AddWithValue("@procid", procID);
                sqlCommand.Parameters.AddWithValue("@msgid", msgID);
                sqlCommand.Parameters.AddWithValue("@msgtimestamp", timestamp.UtcDateTime);
                sqlCommand.Parameters.AddWithValue("@msgoffset", timestamp.Offset.ToString());
                if (thissdid == 0)
                {
                    sqlCommand.Parameters.AddWithValue("@sdid", null);
                }
                else
                    sqlCommand.Parameters.AddWithValue("@sdid", thissdid);
                sqlCommand.Parameters.AddWithValue("@msg", msg);
                sqlCommand.ExecuteNonQuery();
            }
        }
    }

}

