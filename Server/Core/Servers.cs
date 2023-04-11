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
        public string LocalIP { get; set; } = default!;
        public string PublicIP { get; set; } = default!;
        public int Port { get; set; }

        public bool IsServerLocal { get; set; }

        public int MaxConnections { get; set; }
        public int CurrentConnections { get; set; }

        //TCP
        public TcpListener Server { get; set; } = default!;
        public TcpClient Client { get; set; } = default!;
        public NetworkStream Stream { get; set; } = default!;

        //Udp shit
        //public int Udp_port { get; set; }
        public int UdpPort { get; set; }
        public UdpClient Udp_Client { get; set; } = default!;
        public List<byte[]> Udp_Queue { get; set; } = new List<byte[]>();
    }
}
