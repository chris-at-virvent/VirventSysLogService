using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirventPluginContract
{
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