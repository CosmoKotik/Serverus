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
        public string LocalIP { get; set; } = default!;
        public string PublicIP { get; set; } = default!;
        public int Port { get; set; }
        public int UdpPort { get; set; }

        public bool IsServerLocal { get; set; }

        public int MaxConnections { get; set; }
        public int CurrentConnections { get; set; }

        public TcpClient Client { get; set; } = default!;
    }
}
