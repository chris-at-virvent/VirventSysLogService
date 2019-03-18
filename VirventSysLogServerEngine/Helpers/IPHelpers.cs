using System.Net;
using System.Net.Sockets;

namespace VirventSysLogServerEngine.Helpers
{
    public class IPHelpers
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
            return Dns.GetHostEntry(GetLocalAddress());
        }
    }
}
