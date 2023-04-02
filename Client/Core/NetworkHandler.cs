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
        public string MainServerIP { get; set; } = "10.0.0.34";
        public int MainServerPort { get; set; } = 38175;

        public bool IsConnected { get; set; }

        public readonly object ACKPacketsLock = new object();
        public readonly object ACKPacketIdsLock = new object();
        
        public List<Packet> ACKPackets { get; set; } = new List<Packet>();
        public List<int> ACKPacketIds { get; set; } = new List<int>();

        private int _port;
        private UdpClient _client = default!;

        private int _clientID;

        public void HandleConnection()
        {
            _port = new Random().Next(50000, 55000);
            using (_client = new UdpClient())
            {
                //new Thread(() => { HandleACK(); }).Start();

                BufferManager bm = new BufferManager();

                try
                {
                    _client.MulticastLoopback = true;
                    //_client.AllowNatTraversal(true);

                    bm.SetPacketId(0x00);
                    bm.AddInt(_clientID);

                    IPEndPoint groupEP = new IPEndPoint(IPAddress.Parse(MainServerIP), MainServerPort);
                    SendWithACK(bm.GetBytes(), groupEP);

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

                            switch (packetId)
                            {
                                //Redirect to specific server
                                case 0x00:
                                    string srvIP = bm.GetString();
                                    int srvPort = bm.GetInt();
                                    groupEP = new IPEndPoint(IPAddress.Parse(srvIP), srvPort);
                                    bm.SetPacketId(0x00);
                                    //bm.AddInt(_clientID);
                                    SendWithACK(bm.GetBytes(), groupEP);
                                    break;
                                //Authentication shit
                                case 0x01:
                                    _clientID = bm.GetInt();

                                    bm.SetPacketId(0x01);

                                    SendWithACK(bm.GetBytes(), groupEP);

                                    //Send other crap
                                    break;
                                //Get required data for p2p
                                case 0x02:

                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        #region ACK / Reliable UDP
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

        #endregion 
    }
}
