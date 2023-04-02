using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Server.Core
{
    internal class UdpNetwork
    {
        public TcpNetwork Tcp { get; set; } = default!;

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

        public int Port { get; set; } = 6666;
        public bool IsStarted { get; set; } = false;

        public const int MAX_BUFFER_SIZE = 1024;
        public const int TIMEOUT = 500;

        private string _localIp = "10.0.1.3";
        private UdpClient _server = new UdpClient();


        public void StartServer()
        {
            Port = new Random().Next(50000, 55000);
            _localIp = GetLocalIPAddress();
            //Console.Write($" UDP:{Port}");

            new Thread(() => { HandleACK(); }).Start();

            IPEndPoint bind = new IPEndPoint(IPAddress.Parse(_localIp), Port);
            using (_server = new UdpClient(bind))
            {
                _server.MulticastLoopback = true;
                //_server.AllowNatTraversal(true);

                IsStarted = true;

                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, Port);
                BufferManager bm = new BufferManager();

                try
                {
                    while (true)
                    {
                        byte[] bytes = _server.Receive(ref groupEP);
                        Console.WriteLine(bytes);
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

                        //New client connected bs
                        switch (packetId)
                        {
                            //Start authentication packet
                            case 0x00:
                                if (Tcp.ServerInfo.SrvType.Equals(Servers.ServerType.Auth))
                                {
                                    int clientId = new Random().Next(1, int.MaxValue);
                                    bm.SetPacketId(0x01);
                                    bm.AddInt(clientId);
                                }
                                break;
                            //Authentication crap
                            case 0x01:

                                //Authentication failed
                                SendWithACK(BitConverter.GetBytes(0xff), groupEP);

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

                    ep = new IPEndPoint(IPAddress.Parse(OtherServers[index].IP), OtherServers[index].Port);
                }

                if (hasReceived)
                { 
                    
                }

                if (Console.KeyAvailable)
                {
                    int puid = new Random().Next(1, 55555);
                    bm.SetPacketId(0x01);
                    bm.AddInt(puid);
                    bm.AddString(Console.ReadKey().KeyChar.ToString());
                    //_server.Send(bm.GetBytes(), ep);
                    BroadcastWithACK(bm.GetBytes(), puid);
                }

                Thread.Sleep(1);
            }
        }

        #region ACK / Reliable UDP

        private void BroadcastWithACK(byte[] bytes, int puid, bool includeSelf = false)
        {
            foreach (IPEndPoint ep in ConnectedEP)
            {
                if (!ep.Equals(new IPEndPoint(IPAddress.Parse(_localIp), Port)))
                {
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
                Thread.Sleep(200);
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
