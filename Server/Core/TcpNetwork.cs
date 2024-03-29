﻿using Server.Modules;
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
using static Server.Core.Servers;

namespace Server.Core
{
    internal class TcpNetwork
    {
        public UdpNetwork Udp { get; set; } = default!;

        public List<Servers> OtherServers { get; set; } = new List<Servers>();

        public Servers ServerInfo { get; set; } = default!;

        private TcpClient _client = default!;
        private NetworkStream _stream = default!;
        private BufferManager _bufferManager = new BufferManager();

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private int _mainSrvPort = 38174;
        private string _mainSrvIP = "10.0.0.3";

        private int _port;
        private string _localIp = default!;

        internal void StartServer()
        {
            //_mainSrvIP = "10.0.0.34";

            try
            {
                while (!Udp.IsStarted) { Thread.Sleep(1); }
                _port = new Random().Next(50000, 51000);
                _localIp = GetLocalIPAddress();
                _client = new TcpClient();
                _client.Connect(IPAddress.Parse(_mainSrvIP), _mainSrvPort);
                //while (!_client.Connected) { Thread.Sleep(1); }
                _client.ReceiveBufferSize = MAX_BUFFER_SIZE;
                //_client.ReceiveTimeout = TIMEOUT;

                _stream = _client.GetStream();

                new Thread(() => { HandleP2P(); }).Start();
                while (ServerInfo == null) { Thread.Sleep(1); }

                //Load config
                //ServerInfo.MaxConnections = 2;
                ServerInfo.MaxConnections = int.Parse(Config.GetConfig("maxConnections"));

                Udp.ServerInfo = ServerInfo;

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
                                    ServerInfo.LocalIP = _localIp;
                                    ServerInfo.PublicIP = Config.GetConfig("externalIp");
                                    ServerInfo.Port = _port;
                                    ServerInfo.UdpPort = Udp.Port;

                                    _bufferManager.SetPacketId(0x00);
                                    _bufferManager.AddLong(ServerInfo.ServerID);
                                    _bufferManager.AddString(ServerInfo.LocalIP);
                                    _bufferManager.AddString(ServerInfo.PublicIP);
                                    _bufferManager.AddInt(ServerInfo.Port);
                                    _bufferManager.AddInt(ServerInfo.UdpPort);
                                    _bufferManager.AddInt(ServerInfo.MaxConnections);

                                    _stream.Write(_bufferManager.GetBytes());

                                    _bufferManager.SetPacketId(0x02);
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
                                        LocalIP = _bufferManager.GetString(),
                                        PublicIP = _bufferManager.GetString(),
                                        IsServerLocal = _bufferManager.GetBool(),
                                        Port = _bufferManager.GetInt(),
                                        UdpPort = _bufferManager.GetInt(),
                                        MaxConnections = _bufferManager.GetInt()
                                    };
                                    TcpClient client = new TcpClient();

                                    string srvIP;
                                    if (srvs.IsServerLocal)
                                        srvIP = srvs.LocalIP;
                                    else
                                        srvIP = srvs.PublicIP;

                                    client.Connect(srvIP, srvs.Port);
                                    NetworkStream stream = client.GetStream();

                                    srvs.Client = client;
                                    srvs.Stream = stream;

                                    //Add other auth/game server
                                    Console.WriteLine($"Added new {srvs.SrvType} server from port {srvs.Port}");

                                    _bufferManager.SetPacketId(0x02);
                                    _bufferManager.AddLong(ServerInfo.ServerID);
                                    _bufferManager.AddInt((int)srvs.SrvType);
                                    _bufferManager.AddString(ServerInfo.LocalIP);
                                    _bufferManager.AddString(ServerInfo.PublicIP);
                                    _bufferManager.AddInt(ServerInfo.Port);
                                    _bufferManager.AddInt(Udp.Port);
                                    _bufferManager.AddInt(srvs.MaxConnections);

                                    OtherServers.Add(srvs);

                                    stream.Write(_bufferManager.GetBytes());
                                    break;
                                case 0x02:
                                    //Add peers
                                    int peerAmount = _bufferManager.GetInt();
                                    for (int i = 0; i < peerAmount; i++)
                                    {
                                        Client peer = new Client()
                                        {
                                            ClientID = _bufferManager.GetInt(),
                                            ServerID = _bufferManager.GetLong(),
                                            ClientEndPoint = new IPEndPoint(IPAddress.Parse(_bufferManager.GetString()), _bufferManager.GetInt())
                                        };

                                        if (!Udp.ConnectedClients.Contains(peer))
                                            Udp.ConnectedClients.Add(peer);
                                        Console.WriteLine($"Added new client: {peer.ClientEndPoint.ToString()}");
                                    }
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

        public void SendConnAmount(int connAmount)
        {
            Task t = Task.Factory.StartNew(() => 
            {
                ServerInfo.CurrentConnections = connAmount;
                BufferManager bm = new BufferManager();
                bm.SetPacketId(0x05);
                bm.AddLong(ServerInfo.ServerID);
                bm.AddInt(connAmount);

                //_stream.Write(bm.GetBytes());

                for (int i = 0; i < OtherServers.Count; i++)
                {
                    OtherServers[i].Stream.Write(bm.GetBytes());
                }
            });
        }

        #region Send: Add connection / Remove Connection
        public void SendAddClient(int clientId, IPEndPoint ep)
        {
            Task t = Task.Factory.StartNew(() =>
            {
                BufferManager bm = new BufferManager();
                bm.SetPacketId(0x06);
                bm.AddLong(ServerInfo.ServerID);
                bm.AddInt(clientId);
                bm.AddString(ep.Address.ToString());
                bm.AddInt(ep.Port);

                _stream.Write(bm.GetBytes());

                for (int i = 0; i < OtherServers.Count; i++)
                {
                    OtherServers[i].Stream.Write(bm.GetBytes());
                    Thread.Sleep(10);
                }
            });
        }
        public void SendRemoveClient(int clientId)
        {
            Task t = Task.Factory.StartNew(() =>
            {
                BufferManager bm = new BufferManager();
                bm.SetPacketId(0x07);
                bm.AddLong(ServerInfo.ServerID);
                bm.AddInt(clientId);

                _stream.Write(bm.GetBytes());

                for (int i = 0; i < OtherServers.Count; i++)
                {
                    OtherServers[i].Stream.Write(bm.GetBytes());
                    Thread.Sleep(10);
                }
            });
        }
        #endregion

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

            //Console.WriteLine("Wow u somehow managed to close it, amazing");
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

                        long serverId;
                        int clientId;

                        switch (pid)
                        {
                            case 0x02:
                                Servers srv = new Servers()
                                {
                                    ServerID = bm.GetLong(),
                                    SrvType = (Servers.ServerType)bm.GetInt(),
                                    LocalIP = bm.GetString(),
                                    PublicIP = bm.GetString(),
                                    IsServerLocal = IPChecker.IsPrivate(clientEP!.Address.ToString()),
                                    Port = bm.GetInt(),
                                    UdpPort = bm.GetInt(),
                                    MaxConnections = bm.GetInt()
                                };

                                int index = 0;

                                lock (Udp.OtherServersLock)
                                {
                                    Udp.OtherServers.Add(srv);
                                    lock (Udp.ConnectedEPLock)
                                        Udp.ConnectedEP.Add(new IPEndPoint(clientEP.Address, srv.Port));

                                    index = Udp.OtherServers.Count - 1;
                                    //Console.WriteLine("geasrkgjhbawerkguyrewg");
                                }

                                new Thread(() => { Udp.HandleClient(index); }).Start();
                                break;
                            case 0x05:
                                serverId = bm.GetLong();
                                int cc = bm.GetInt();
                                 
                                Servers currentsrv = OtherServers.Find(x => x.ServerID.Equals(serverId))!;

                                Udp.OtherServers.Find(x => x.ServerID.Equals(serverId))!.CurrentConnections = currentsrv.CurrentConnections = cc;
                                //Console.WriteLine($"{serverId}: {cc}/{currentsrv.MaxConnections}");
                                break;
                            //Add new UDP client
                            case 0x06:
                                serverId = bm.GetLong();
                                clientId = bm.GetInt();

                                Client clientInstance = new Client()
                                { 
                                    ServerID = serverId,
                                    ClientID = clientId,
                                    ClientEndPoint = new IPEndPoint(IPAddress.Parse(bm.GetString()), bm.GetInt())
                                };

                                Udp.ConnectedClients.Add(clientInstance);

                                //Console.WriteLine($"New client:{clientId} connected to: {serverId}");
                                break;
                            //Remove UDP client
                            case 0x07:
                                serverId = bm.GetLong();
                                clientId = bm.GetInt();

                                Udp.ConnectedClients.RemoveAll(x => x.ClientID.Equals(clientId));

                                //Console.WriteLine($"Client:{clientId} disconnected from: {serverId}");
                                break;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine($"Server Lost Connection");
                client.Dispose();
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