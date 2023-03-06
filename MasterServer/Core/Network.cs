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
        public List<Server> Servers { get; set; } = new List<Server>();

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private int _port = 38174;
        private string _localIp = "10.0.1.3";

        private TcpListener _server = null;

        public void StartServer()
        {
            _server = new TcpListener(IPAddress.Parse(_localIp), _port);
            _server.Start();
            Console.WriteLine("Server started on: " + _port);

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

                    Server srv = new Server()
                    {
                        ServerID = new Random().NextInt64(),
                        SrvType = srvType,
                        IP = clientEP.Address.ToString(),
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
    }
}
