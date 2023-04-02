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
        public static void Connect()
        {
            NetworkHandler nh = new NetworkHandler();
            nh.HandleConnection();
        }
    }
}
