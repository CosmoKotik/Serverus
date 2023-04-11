using Server.Core;

namespace Server
{
    internal class Program
    {
        public static Thread t1 = default!;
        public static Thread t2 = default!;

        static void Main(string[] args)
        {
            Config.LoadConfigs();

            UdpNetwork udpNet = new UdpNetwork();
            TcpNetwork tcpNet = new TcpNetwork();

            udpNet.Tcp = tcpNet;
            tcpNet.Udp = udpNet;

            t1 = new Thread(() => udpNet.StartServer());
            t2 = new Thread(() => tcpNet.StartServer());

            t1.Start();
            t2.Start();
        }
    }
}