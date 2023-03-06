using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MasterServer.Core.ServerTypes;

namespace MasterServer.Core
{
    internal class Server
    {
        public long ServerID { get; set; }
        public ServerType SrvType { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }

        public TcpClient Client { get; set; }
    }
}
