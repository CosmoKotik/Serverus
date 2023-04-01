using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client.Core
{
    internal class NetworkHandler
    {
        public string? MainServerIP { get; set; } = "10.0.1.3";
        public int? MainServerPort { get; set; } = 38175;

        public bool IsConnected { get; set; }

        public readonly object ACKPacketsLock = new object();
        public readonly object ACKPacketIdsLock = new object();
        
        public List<Packet> ACKPackets { get; set; } = new List<Packet>();
        public List<int> ACKPacketIds { get; set; } = new List<int>();

        private int _port = 0;
        private UdpClient _client;

        private int _clientID = 0;

        public void HandleConnection(IPEndPoint ep)
        {
            _port = new Random().Next(50000, 55000);
            using (_client = new UdpClient())
            {
                _clientID = new Random().Next(1, int.MaxValue);

                //new Thread(() => { HandleACK(); }).Start();

                IPEndPoint groupEP = ep;
                BufferManager bm = new BufferManager();

                try
                {
                    _client.MulticastLoopback = true;
                    _client.AllowNatTraversal(true);

                    bm.SetPacketId(0x00);
                    bm.AddInt(_clientID);

                    SendWithACK(bm.GetBytes(), ep);

                    while (true)
                    {
                        if (_client.Available > 0)
                        {
                            byte[] bytes = _client.Receive(ref groupEP);
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
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /*private void BroadcastWithACK(byte[] bytes, int puid, bool includeSelf = false)
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
        }*/

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
            _client.Send(bytes, ep);
        }

        private void HandleACK()
        {
            while (true)
            {
                for (int i = 0; i < ACKPackets.Count; i++)
                {
                    _client.Send(ACKPackets[i].bytes, ACKPackets[i].EP);
                }
                Thread.Sleep(200);
            }
        }

        private int GetPacketID()
        { 
            return new Random().Next(1, int.MaxValue);
        }
    }
}
