using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    internal class Servers
    {
        public enum ServerType
        {
            Auth,
            Game,
            Voice
        }

        public long ServerID { get; set; }
        public ServerType SrvType { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }

        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
    }
}
