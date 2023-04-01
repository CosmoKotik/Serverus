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
        public string? IP { get; set; }
        public int Port { get; set; }

        public int MaxConnections { get; set; }
        public int CurrentConnections { get; set; }

        //TCP
        public TcpListener? Server { get; set; }
        public TcpClient? Client { get; set; }
        public NetworkStream? Stream { get; set; }

        //Udp shit
        //public int Udp_port { get; set; }
        public UdpClient? Udp_Client { get; set; }
        public List<byte[]> Udp_Queue { get; set; } = new List<byte[]>();
    }
}
