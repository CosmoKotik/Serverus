using Server.Core;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UdpNetwork udpNet = new UdpNetwork();
            TcpNetwork tcpNet = new TcpNetwork();

            udpNet.Tcp = tcpNet;
            tcpNet.Udp = udpNet;

            tcpNet.StartServer();
            //udpNet.StartServer();
        }
    }
}