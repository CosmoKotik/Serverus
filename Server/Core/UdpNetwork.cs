using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Server.Core
{
    internal class UdpNetwork
    {
        public TcpNetwork Tcp { get; set; }
        public List<Servers> OtherServers { get; set; } = new List<Servers>();

        private UdpClient _server = new UdpClient();

        public int Port = 6666;
        public bool IsStarted = false;

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private string _localIp = "127.0.0.1";

        public void StartServer()
        {
            Port = new Random().Next(50000, 55000);
            _localIp = GetLocalIPAddress();

            IPEndPoint bind = new IPEndPoint(IPAddress.Parse(_localIp), Port);
            using (_server = new UdpClient(bind))
            {
                _server.MulticastLoopback = true;
                _server.AllowNatTraversal(true);

                IsStarted = true;

                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, Port);
                BufferManager bm = new BufferManager();

                try
                {
                    while (true)
                    { 
                        
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally 
                { 
                    _server.Dispose(); 
                }
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
