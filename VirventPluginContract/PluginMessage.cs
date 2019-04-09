using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventPluginContract
{
    public class PluginMessage
    {
        public string msg { get; set; }
        public Severities severity { get; set; }
        public Facilities facility { get; set; }
    }
}