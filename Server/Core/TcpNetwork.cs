using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    internal class TcpNetwork
    {
        public UdpNetwork Udp { get; set; }

        public List<Servers> OtherServers { get; set; } = new List<Servers>();

        public Servers ServerInfo { get; set; }

        private TcpClient _client = new TcpClient();
        private NetworkStream _stream = null;
        private BufferManager _bufferManager = new BufferManager();

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private int _mainSrvPort = 38174;
        private string _mainSrvIP = "10.0.1.3";

        private int _port;
        private string _localIp;

        internal void StartServer()
        {
            try
            {
                _client.Connect(IPAddress.Parse(_mainSrvIP), _mainSrvPort);
                //while (!_client.Connected) { Thread.Sleep(1); }
                _client.ReceiveBufferSize = MAX_BUFFER_SIZE;
                //_client.ReceiveTimeout = TIMEOUT;

                _stream = _client.GetStream();

                byte[] bytes = new byte[MAX_BUFFER_SIZE];
                int size;
                while (_client.Connected)
                    //while ((size = _stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    if (_stream.DataAvailable)
                    {
                        while ((size = _stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            //size = _stream.Read(bytes, 0, bytes.Length);
                            _bufferManager.SetBytes(bytes);

                            Servers srvs;

                            switch (_bufferManager.GetPacketId())
                            {
                                case 0x00:
                                    ServerInfo = new Servers()
                                    {
                                        ServerID = _bufferManager.GetLong(),
                                        SrvType = (Servers.ServerType)_bufferManager.GetInt(),
                                        IP = _localIp,
                                        Port = _port
                                    };
                                    Console.WriteLine($"Runnins as {ServerInfo.SrvType}");
                                    break;
                                case 0x01:
                                    srvs = new Servers()
                                    {
                                        ServerID = _bufferManager.GetLong(),
                                        SrvType = (Servers.ServerType)_bufferManager.GetInt(),
                                        IP = _bufferManager.GetString(),
                                        Port = _bufferManager.GetInt()
                                    };
                                    //Add other auth/game server
                                    Console.WriteLine($"Added new {srvs.SrvType} server");
                                    OtherServers.Add(srvs);
                                    break;
                                case 0x02:
                                    srvs = new Servers()
                                    {
                                        ServerID = _bufferManager.GetLong(),
                                        SrvType = (Servers.ServerType)_bufferManager.GetInt(),
                                        IP = _bufferManager.GetString(),
                                        Port = _bufferManager.GetInt()
                                    };
                                    Console.WriteLine($"Added new {srvs.SrvType} server111");
                                    OtherServers.Add(srvs);
                                    break;
                            }
                        }
                    }

                    //Thread.Sleep(1);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                _stream.Close();
                _client.Close();
            }
        }
    }
}
