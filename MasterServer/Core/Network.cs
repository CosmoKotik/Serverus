using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MasterServer.Core.ServerTypes;

namespace MasterServer.Core
{
    internal class Network
    {
        public UdpHandler Udp = default!;

        public bool Started { get; set; }

        public List<Server> Servers { get; set; } = new List<Server>();
        public List<int> AvailableServers { get; set; } = new List<int>();

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private int _port = 38174;
        private string _localIp = "10.0.0.3";

        private TcpListener? _server;

        public void StartServer()
        {
            _localIp = GetLocalIPAddress();
            _server = new TcpListener(IPAddress.Parse(_localIp), _port);
            _server.Start();
            Console.WriteLine("Server started on: " + _port);
            Started = true;

            byte[] bytes = new byte[MAX_BUFFER_SIZE];

            try
            {
                while (true)
                {
                    TcpClient client = _server.AcceptTcpClient();
                    ServerType srvType = ServerType.Auth;
                    IPEndPoint? clientEP = client.Client.RemoteEndPoint as IPEndPoint;

                    ClientHandler clientHandler = new ClientHandler(this);

                    if (Servers.Any(x => x.SrvType == ServerType.Auth))
                        srvType = ServerType.Game;

                    long serverId = new Random().NextInt64();
                    if (Servers.Any(x => x.ServerID.Equals(serverId)))
                        serverId = new Random().NextInt64();

                    Server srv = new Server()
                    {
                        ServerID = serverId,
                        SrvType = srvType,
                        IP = clientEP!.Address.ToString(),
                        Port = clientEP.Port,
                        Client = client
                    };

                    new Thread(() => { clientHandler.NetworkHandler(client, srv); }).Start();

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _server.Stop();
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
