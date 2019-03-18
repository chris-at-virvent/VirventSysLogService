using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnortPlugin
{
    public class SnortPlugin : IPlugin
    {
        public string Name
        {
            get
            {
                return "Snort Definition Processor";
            }
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}
