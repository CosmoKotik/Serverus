using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.Core
{
    internal class Network
    {
        public static void Connect(string hostname, int port)
        {
            NetworkHandler nh = new NetworkHandler();
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(hostname), port);
            nh.HandleConnection(ep);
        }
    }
}
