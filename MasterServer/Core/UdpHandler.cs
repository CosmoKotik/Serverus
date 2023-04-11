using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MasterServer.Modules;

namespace MasterServer.Core
{
    internal class UdpHandler
    {
        public Network Net { get; set; } = default!;
        public List<Client> ClientPeers = new List<Client>();

        public readonly object OtherServersLock = new object();
        public readonly object ConnectedEPLock = new object();
        public readonly object ACKPacketsLock = new object();
        public readonly object ACKPacketIdsLock = new object();
        public readonly object ServerLock = new object();

        public List<Packet> ACKPackets { get; set; } = new List<Packet>();
        public List<int> ACKPacketIds { get; set; } = new List<int>();

        private int _port = 38175;
        private string _localIp = "10.0.0.3";
        private UdpClient _server = default!;

        public UdpHandler(Network n)
        {
            Net = n;
        }

        public void StartServer()
        {
            while (!Net.Started) { Thread.Sleep(1); }
            Net.Udp = this;

            _localIp = GetLocalIPAddress();
            Console.WriteLine(_localIp);
            IPEndPoint bind = new IPEndPoint(IPAddress.Parse(_localIp), _port);
            using (_server = new UdpClient(bind))
            {
                _server.MulticastLoopback = true;
                //_server.AllowNatTraversal(true);

                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, _port);
                BufferManager bm = new BufferManager();

                try
                {
                    while (true)
                    {
                        byte[] bytes = _server.Receive(ref groupEP);
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

                        _server.Send(bytes, groupEP);

                        bool isIPLocal = IPChecker.IsPrivate(groupEP.Address.ToString());

                        //New client connected bs
                        switch (packetId)
                        {
                            case 0x00:
                                for (int i = 1; i < Net.Servers.Count; i++)
                                {
                                    if (Net.Servers[i - 1].CurrentConnections <= Net.Servers[i].CurrentConnections)
                                    {
                                        Server srv = Net.Servers[i - 1];
                                        bm.SetPacketId(0x00);
                                        
                                        if (isIPLocal)
                                            bm.AddString(srv.LocalIP);
                                        else
                                            bm.AddString(srv.PublicIP);
                                        
                                        bm.AddInt(srv.UdpPort);
                                        SendWithACK(bm.GetBytes(), groupEP);
                                        //_server.Send(bm.GetBytes(), groupEP);
                                        break;
                                    }
                                }
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