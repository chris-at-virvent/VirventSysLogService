using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventPluginContract
{
    public interface IPlugin
    {
        string Name { get; }
        void Run(List<PluginSetting> settings, out List<PluginMessage> messages);
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
