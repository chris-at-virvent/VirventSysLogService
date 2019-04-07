using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnortPlugin
{
    public class SnortSettings
    {
        public string SnortRemoteServer { get; set; }
        public string SnortVersion { get; set; }
        public string SnortPath { get; set; }
        public string DownloadPath { get; set; }
        public string UnpackPath { get; set; }
        public string SnortSubscrptionLevel { get; set; }
        public string OinkCode { get; set; }
        public List<string> ServiceEndpoints { get; set; }
        public string BlackListURL { get; set; }
    }
}
