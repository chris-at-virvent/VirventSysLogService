using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirventPluginContract
{
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
}