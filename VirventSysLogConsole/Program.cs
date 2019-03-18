using System;
// using System.ServiceProcess;
using VirventSysLogServerEngine;

namespace VirventSysLogConsole
{
    class VirventSysLog
    {

        static void Main(string[] args)
        {


            Console.WriteLine("Initializing Server");
            Engine engine = new Engine(true, true);

            // in console mode - automatically switch to debug logging levels

            while (true)
            {

            }

        }
    }
}
