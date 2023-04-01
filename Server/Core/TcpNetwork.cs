using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    internal class TcpNetwork
    {
        public UdpNetwork? Udp { get; set; }

        public List<Servers> OtherServers { get; set; } = new List<Servers>();

        public Servers? ServerInfo { get; set; }

        private TcpClient? _client;
        private NetworkStream _stream = null;
        private BufferManager _bufferManager = new BufferManager();

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private int _mainSrvPort = 38174;
        private string _mainSrvIP = "10.0.1.3";

        private int _port;
        private string? _localIp;

        internal void StartServer()
        {
            _mainSrvIP = "10.0.0.34";

            try
            {
                while (!Udp.IsStarted) { Thread.Sleep(1); }
                _port = new Random().Next(50000, 51000);
                _localIp = "10.0.1.3";
                _client = new TcpClient();
                _client.Connect(IPAddress.Parse(_mainSrvIP), _mainSrvPort);
                //while (!_client.Connected) { Thread.Sleep(1); }
                _client.ReceiveBufferSize = MAX_BUFFER_SIZE;
                //_client.ReceiveTimeout = TIMEOUT;

                _stream = _client.GetStream();

                new Thread(() => { HandleP2P(); }).Start();
                while (ServerInfo == null) { Thread.Sleep(1); }

                ServerInfo.MaxConnections = 2;

                byte[] bytes = new byte[MAX_BUFFER_SIZE];
                int size;
                while (true)
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
                                    /*ServerInfo = new Servers()
                                    {
                                        ServerID = _bufferManager.GetLong(),
                                        SrvType = (Servers.ServerType)_bufferManager.GetInt(),
                                        IP = _localIp,
                                        Port = _port,
                                        //Stream = _stream,
                                        //Client = _client
                                    };*/

                                    ServerInfo.ServerID = _bufferManager.GetLong();
                                    ServerInfo.SrvType = (Servers.ServerType)_bufferManager.GetInt();
                                    ServerInfo.IP = _localIp;
                                    ServerInfo.Port = _port;

                                    _bufferManager.SetPacketId(0x00);
                                    _bufferManager.AddLong(ServerInfo.ServerID);
                                    _bufferManager.AddString(ServerInfo.IP);
                                    _bufferManager.AddInt(ServerInfo.Port);
                                    _bufferManager.AddInt(ServerInfo.MaxConnections);

                                    _stream.Write(_bufferManager.GetBytes());

                                    //IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ServerInfo.IP), _port);
                                    //ServerInfo.Client = new TcpListener(ep);
                                    //ServerInfo.Stream = ServerInfo.Client.AcceptTcpClient().GetStream();

                                    Console.WriteLine($"Runnins as {ServerInfo.SrvType}:{ServerInfo.ServerID} at TCP:{ServerInfo.Port} UDP:{Udp.Port}");
                                    break;
                                case 0x01:
                                    srvs = new Servers()
                                    {
                                        ServerID = _bufferManager.GetLong(),
                                        SrvType = (Servers.ServerType)_bufferManager.GetInt(),
                                        IP = _bufferManager.GetString(),
                                        Port = _bufferManager.GetInt(),
                                        MaxConnections = _bufferManager.GetInt()
                                    };
                                    TcpClient client = new TcpClient();
                                    client.Connect(srvs.IP, srvs.Port);
                                    //Console.WriteLine(client.Client.RemoteEndPoint);
                                    NetworkStream stream = client.GetStream();

                                    srvs.Client = client;
                                    srvs.Stream = stream;

                                    //srvs.Client = new TcpClient(srvs.IP, srvs.Port);
                                    //srvs.Stream = srvs.Client.GetStream();

                                    //Add other auth/game server
                                    Console.WriteLine($"Added new {srvs.SrvType} server from port {srvs.Port}");

                                    _bufferManager.SetPacketId(0x02);
                                    _bufferManager.AddLong(ServerInfo.ServerID);
                                    _bufferManager.AddInt((int)srvs.SrvType);
                                    //_bufferManager.AddString(ServerInfo.IP);
                                    _bufferManager.AddInt(Udp.Port);

                                    OtherServers.Add(srvs);

                                    stream.Write(_bufferManager.GetBytes());
                                    break;
                                case 0x02:
                                    
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
                _stream.Dispose();
                _client.Dispose();
            }
        }

        public async Task SendConnAmount()
        {
            await Task.Run(() => 
            {
                BufferManager bm = new BufferManager();
                bm.SetPacketId(0x05);
                bm.AddLong(ServerInfo.ServerID);
                bm.AddInt(ServerInfo.CurrentConnections);

                _stream.Write(bm.GetBytes());

                for (int i = 0; i < OtherServers.Count; i++)
                {
                    OtherServers[i].Stream.Write(bm.GetBytes());
                }
            });
        }

        private void HandleP2P()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(_localIp), _port);
            ServerInfo = new Servers()
            {
                Server = new TcpListener(IPAddress.Parse(_localIp), _port)
            };
            ServerInfo.Server.Start();

            while (true)
            {
                TcpClient client = ServerInfo.Server.AcceptTcpClient();

                new Thread(() => { HandlePeer(client); }).Start();
                
                Thread.Sleep(1);
            }

            Console.WriteLine("Wow u somehow managed to close it, amazing");
        }

        private void HandlePeer(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            IPEndPoint? clientEP = client.Client.RemoteEndPoint as IPEndPoint;

            int size;
            byte[] bytes = new byte[MAX_BUFFER_SIZE];
            BufferManager bm = new BufferManager();
            //Console.WriteLine(clientEP.Port);

            try
            {
                while (client.Connected)
                {
                    while ((size = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        bm.SetBytes(bytes);
                        int pid = bm.GetPacketId();
                        switch (pid)
                        {
                            case 0x02:
                                Servers srv = new Servers()
                                {
                                    ServerID = bm.GetLong(),
                                    SrvType = (Servers.ServerType)bm.GetInt(),
                                    IP = clientEP.Address.ToString(),
                                    Port = bm.GetInt()
                                };

                                int index = 0;

                                lock (Udp.OtherServersLock)
                                {
                                    Udp.OtherServers.Add(srv);
                                    lock (Udp.ConnectedEPLock)
                                        Udp.ConnectedEP.Add(new IPEndPoint(clientEP.Address, srv.Port));

                                    index = Udp.OtherServers.Count - 1;
                                }

                                new Thread(() => { Udp.HandleClient(index); }).Start();
                                break;
                            case 0x05:
                                long serverId = bm.GetLong();
                                int cc = bm.GetInt();

                                OtherServers.Find(x => x.ServerID.Equals(serverId)).CurrentConnections = cc;
                                Console.WriteLine($"{serverId}: {cc}/{OtherServers.Find(x => x.ServerID.Equals(serverId)).MaxConnections}");
                                break;
                        }
                    }
                }
            }
            catch
            {

            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
