using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using static Server.Core.Servers;
using Server.Modules;

namespace Server.Core
{
    internal class UdpNetwork
    {
        public TcpNetwork Tcp { get; set; } = default!;
        public Servers ServerInfo { get; set; } = default!;

        public readonly object OtherServersLock = new object();
        public readonly object ConnectedEPLock = new object();
        public readonly object ACKPacketsLock = new object();
        public readonly object ACKPacketIdsLock = new object();
        public readonly object ServerLock = new object();
        public List<Servers> OtherServers { get; set; } = new List<Servers>();
        public List<IPEndPoint> ConnectedEP { get; set; } = new List<IPEndPoint>();
        public List<Packet> ACKPackets { get; set; } = new List<Packet>();
        public List<int> ACKPacketIds { get; set; } = new List<int>();

        public List<Client> ConnectedClients { get; set; } = new List<Client>();

        IPEndPoint groupEP = default!;

        public int Port { get; set; } = 6666;
        public bool IsStarted { get; set; } = false;

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private string _localIp = "10.0.0.3";
        private UdpClient _server = new UdpClient();


        public void StartServer()
        {
            ServerInfo = new Servers();
            Port = new Random().Next(50000, 55000);
            ServerInfo.UdpPort = Port;
            
            _localIp = GetLocalIPAddress();
            //Console.Write($" UDP:{Port}");

            new Thread(() => { HandleACK(); }).Start();

            IPEndPoint bind = new IPEndPoint(IPAddress.Parse(_localIp), Port);
            using (_server = new UdpClient(bind))
            {
                _server.MulticastLoopback = true;
                //_server.AllowNatTraversal(true);

                IsStarted = true;

                groupEP = new IPEndPoint(IPAddress.Any, Port);
                BufferManager bm = new BufferManager();

                try
                {
                    while (true)
                    {
                        byte[] bytes = _server.Receive(ref groupEP);
                        //Console.WriteLine(bytes);
                        lock (ACKPacketsLock)
                            if (ACKPackets.Any(x => x.EP.Equals(groupEP) && x.bytes.SequenceEqual(bytes)))
                            {
                                ACKPackets.RemoveAll(x => x.EP.Equals(groupEP) && x.bytes.SequenceEqual(bytes));
                                continue;
                            }

                        bm.SetBytes(bytes);
                        int packetId = bm.GetPacketId();
                        int puid = bm.GetInt();

                        lock (ACKPacketIdsLock)
                            if (ACKPacketIds.Any(x => x.Equals(puid)))
                                continue;

                        lock (ConnectedEPLock)
                            if (ConnectedEP.Any(x => x.Equals(groupEP)))
                            {
                                _server.Send(bytes, groupEP);

                                int index = ConnectedEP.FindIndex(x => x.Equals(groupEP));

                                lock (OtherServersLock)
                                    OtherServers[index].Udp_Queue.Add(bytes);
                                continue;
                            }

                        _server.Send(bytes, groupEP);

                        bool isIPLocal = IPChecker.IsPrivate(groupEP.Address.ToString());           //Checks if client is local or no

                        int clientId;
                        int redirectClientId;
                        long redirectServerId;
                        byte[] redirectBytes;
                        Servers redirectServer;
                        IPEndPoint redirectServerEndPoint;

                        //New client connected bs
                        switch (packetId)
                        {
                            //Start authentication packet
                            case 0x00:
                                switch (ServerInfo.SrvType)
                                {
                                    case ServerType.Auth:
                                        clientId = new Random().Next(1, int.MaxValue);
                                        bm.SetPacketId(0x01);
                                        bm.AddInt(clientId);
                                        SendWithACK(bm.GetBytes(), groupEP);

                                        Console.WriteLine($"{clientId} is trying to connect");
                                        break;
                                    case ServerType.Game:
                                        clientId = bm.GetInt();
                                        Client client = new Client()
                                        {
                                            ClientID = clientId,
                                            ServerID = ServerInfo.ServerID,
                                            ClientEndPoint = groupEP
                                        };

                                        bm.SetPacketId(0x02);
                                        bm.AddLong(ServerInfo.ServerID);
                                        bm.AddInt(ConnectedClients.Count);
                                        for (int i = 0; i < ConnectedClients.Count; i++)
                                        {
                                            bm.AddInt(ConnectedClients[i].ClientID);
                                            bm.AddLong(ConnectedClients[i].ServerID);
                                        }

                                        SendWithACK(bm.GetBytes(), groupEP);

                                        for (int i = 0; i < ConnectedClients.Count; i++)
                                        {
                                            bm.SetPacketId(0x02);
                                            bm.AddLong(ServerInfo.ServerID);
                                            bm.AddInt(1);

                                            bm.AddInt(client.ClientID);
                                            bm.AddLong(client.ServerID);

                                            redirectBytes = (byte[])bm.GetBytes().Clone();

                                            if (ConnectedClients[i].ServerID.Equals(client.ServerID))
                                            {
                                                Console.WriteLine($"Sending to {ConnectedClients[i].ClientID}");
                                                SendWithACK(redirectBytes, ConnectedClients[i].ClientEndPoint);
                                                continue;
                                            }
                                            
                                            Console.WriteLine($"Sending to {ConnectedClients[i].ClientID}");
                                            redirectServer = OtherServers.Find(x => x.ServerID.Equals(ConnectedClients[i].ServerID))!;

                                            if (redirectServer.IsServerLocal)
                                                redirectServerEndPoint = new IPEndPoint(IPAddress.Parse(redirectServer.LocalIP)!, redirectServer.UdpPort);
                                            else
                                                redirectServerEndPoint = new IPEndPoint(IPAddress.Parse(redirectServer.PublicIP)!, redirectServer.UdpPort);

                                            bm.SetPacketId(0x04);
                                            bm.AddInt(ConnectedClients[i].ClientID);
                                            bm.InsertBytes(redirectBytes);
                                            SendWithACK(bm.GetBytes(), redirectServerEndPoint);

                                            //SendWithACK(bm.GetBytes(), groupEP);
                                        }

                                        ServerInfo.CurrentConnections++;

                                        ConnectedClients.Add(client);
                                        Tcp.SendConnAmount(ServerInfo.CurrentConnections);
                                        Tcp.SendAddClient(clientId, client.ClientEndPoint);

                                        Console.WriteLine($"{clientId} Connected successfully {ServerInfo.CurrentConnections}/{ServerInfo.MaxConnections}");

                                        break;
                                }
                                break;
                            //Authentication crap
                            case 0x01:
                                switch (ServerInfo.SrvType)
                                {
                                    case ServerType.Auth:
                                        if (true.Equals(false))                  //CHANGE THIS WITH AUTHENTICATION VERIFICATION (NOT IMPLEMENTED)
                                        {
                                            //Authentication failed
                                            bm.AddString("Failed Authentication");
                                            SendWithACK(BitConverter.GetBytes(0xff), groupEP);
                                            break;
                                        }

                                        clientId = bm.GetInt();
                                        bool success = false;

                                        //Find best server
                                        for (int i = 0; i < OtherServers.Count; i++)
                                        {
                                            if (i == OtherServers.Count - 1)
                                            {
                                                if (OtherServers[i].CurrentConnections < OtherServers[i].MaxConnections)
                                                {
                                                    Servers srv = OtherServers[i];
                                                    success = true;
                                                    bm.SetPacketId(0x00);
                                                    if (isIPLocal)
                                                        bm.AddString(srv.LocalIP);
                                                    else
                                                        bm.AddString(srv.PublicIP);
                                                    bm.AddInt(srv.UdpPort);
                                                    SendWithACK(bm.GetBytes(), groupEP);
                                                    break;
                                                }

                                                break;
                                            }

                                            if (OtherServers[i].CurrentConnections <= OtherServers[i + 1].CurrentConnections)
                                            {
                                                //Redirect to specific server
                                                Servers srv = OtherServers[i];
                                                if (srv.CurrentConnections < srv.MaxConnections)
                                                {
                                                    success = true;
                                                    bm.SetPacketId(0x00);
                                                    if (isIPLocal)
                                                        bm.AddString(srv.LocalIP);
                                                    else
                                                        bm.AddString(srv.PublicIP);
                                                    bm.AddInt(srv.UdpPort);
                                                    SendWithACK(bm.GetBytes(), groupEP);
                                                    break;
                                                }
                                            }
                                        }

                                        //Could not find a server, everything is full or unavailable
                                        if (success)
                                        {
                                            Console.WriteLine($"{clientId} Authenticated successfully");
                                            break;
                                        }

                                        Console.WriteLine($"Disconnecting {clientId}: Server is full or unavailable");
                                        bm.SetPacketId(0xff);
                                        bm.AddString("Server is full or unavailable");
                                        SendWithACK(bm.GetBytes(), groupEP);
                                        break;
                                }

                                break;
                            //Redirect packet
                            case 0x03:
                                redirectClientId = bm.GetInt();
                                redirectServerId = bm.GetLong();
                                redirectBytes = bm.GetBytes();
                                Console.WriteLine(BitConverter.ToString(redirectBytes).Replace("-", " "));
                                if (redirectServerId.Equals(ServerInfo.ServerID))
                                {
                                    SendWithACK(redirectBytes, ConnectedClients.Find(x => x.ClientID.Equals(redirectClientId))!.ClientEndPoint);
                                    break;
                                }

                                redirectServer = OtherServers.Find(x => x.ServerID.Equals(redirectServerId))!;
                                if (isIPLocal)
                                    redirectServerEndPoint = new IPEndPoint(IPAddress.Parse(redirectServer.LocalIP)!, redirectServer.UdpPort);
                                else
                                    redirectServerEndPoint = new IPEndPoint(IPAddress.Parse(redirectServer.PublicIP)!, redirectServer.UdpPort);
                                bm.SetPacketId(0x04);
                                bm.AddInt(redirectClientId);
                                bm.InsertBytes(redirectBytes);
                                SendWithACK(bm.GetBytes(), redirectServerEndPoint);
                                break;
                            //Receive Redirected packet
                            case 0x04:
                                redirectClientId = bm.GetInt();
                                redirectBytes = bm.GetBytes();
                                Console.WriteLine(BitConverter.ToString(redirectBytes).Replace("-", " "));

                                SendWithACK(redirectBytes, ConnectedClients.Find(x => x.ClientID.Equals(redirectClientId))!.ClientEndPoint);
                                break;
                            default:
                                Console.WriteLine($"{groupEP.Address}:{groupEP.Port} tried to send wrong data. What do we do now?");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally 
                {
                    ACKPackets.Clear();
                    _server.Dispose(); 
                }
            }
        }

        public void HandleClient(int index)
        {
            BufferManager bm = new BufferManager();
            while (true)
            {
                IPEndPoint ep;
                byte[] receivedData;
                bool hasReceived = false;
                lock (OtherServersLock)
                {
                    if (OtherServers[index].Udp_Queue.Count > 0)
                    {
                        hasReceived = true;
                        receivedData = OtherServers[index].Udp_Queue[0];
                        OtherServers[index].Udp_Queue.RemoveAt(0);

                        bm.SetBytes(receivedData);
                        bm.GetPacketId();
                        bm.GetInt();
                        Console.WriteLine(bm.GetString());
                    }

                    if (OtherServers[index].IsServerLocal)
                    ep = new IPEndPoint(IPAddress.Parse(OtherServers[index].LocalIP), OtherServers[index].Port);
                    else
                    ep = new IPEndPoint(IPAddress.Parse(OtherServers[index].PublicIP), OtherServers[index].Port);
                }

                if (hasReceived)
                { 
                    
                }

                /*if (Console.KeyAvailable)
                {
                    int puid = new Random().Next(1, 55555);
                    bm.SetPacketId(0x01);
                    bm.AddInt(puid);
                    bm.AddString(Console.ReadKey().KeyChar.ToString());
                    //_server.Send(bm.GetBytes(), ep);
                    BroadcastWithACK(bm.GetBytes(), puid);
                }*/

                Thread.Sleep(1);
            }
        }

        #region ACK / Reliable UDP

        private void BroadcastWithACK(byte[] bytes, bool includeSelf = false)
        {
            BufferManager bm = new BufferManager();

            for (int i = 0; i < ConnectedClients.Count; i++)
            {
                //int puid = GetPacketID();
                //bytes = BufferManager.SetPacketUid(puid, bytes);

                bm.SetPacketId(0x03);
                bm.AddInt(ConnectedClients[i].ClientID);
                bm.AddLong(ConnectedClients[i].ServerID);
                bm.InsertBytes(bytes);

                int puid = GetPacketID();
                byte[] redirectBytes = BufferManager.SetPacketUid(puid, bm.GetBytes());

                Packet p = new Packet()
                {
                    bytes = redirectBytes,
                    EP = groupEP,
                    PacketUID = puid
                };

                lock (ACKPacketsLock)
                    ACKPackets.Add(p);
                lock (ACKPacketIdsLock)
                    ACKPacketIds.Add(puid);
                _server.Send(redirectBytes, groupEP);
            }
        }

        private void SendWithACK(byte[] bytes, IPEndPoint ep)
        {
            int puid = GetPacketID();
            bytes = BufferManager.SetPacketUid(puid, bytes);

            Packet p = new Packet()
            {
                bytes = bytes,
                EP = ep,
                PacketUID = puid
            };
            lock (ACKPacketsLock)
                ACKPackets.Add(p);
            lock (ACKPacketIdsLock)
                ACKPacketIds.Add(puid);
            _server.Send(bytes, ep);
        }

        private void HandleACK()
        {
            while (true)
            {
                for (int i = 0; i < ACKPackets.Count; i++)
                {
                    _server.Send(ACKPackets[i].bytes, ACKPackets[i].EP);
                }
                Thread.Sleep(350);
            }
        }

        private int GetPacketID()
        {
            return new Random().Next(1, int.MaxValue);
        }

        #endregion

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
