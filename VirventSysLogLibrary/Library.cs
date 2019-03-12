using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VirventSysLogLibrary
{
    public class Library
    {
        public static IPAddress GetLocalAddress()
        {
            IPAddress localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
            }

            return localIP;
        }

        public static IPHostEntry GetLocalHost()
        {
            return Dns.GetHostEntry(Library.GetLocalAddress());
        }
    }
}
