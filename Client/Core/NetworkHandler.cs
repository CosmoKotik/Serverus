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
        public string MainServerIP { get; set; } = "10.0.0.3";
        public int MainServerPort { get; set; } = 38175;

        public bool IsConnected { get; set; }

        public readonly object ACKPacketsLock = new object();
        public readonly object ACKPacketIdsLock = new object();
        
        public List<Packet> ACKPackets { get; set; } = new List<Packet>();
        public List<int> ACKPacketIds { get; set; } = new List<int>();

        private int _port;
        private UdpClient _client = default!;
        private IPEndPoint _remoteEndPoint = default!;
        
        private int _clientID;
        private long _connectedServerID;

        private bool _isAuthenticated = false;

        private List<Peer> _peers = new List<Peer>();

        public void HandleConnection()
        {
            new Thread(() => { poopoo(); }).Start();

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

                    _remoteEndPoint = new IPEndPoint(IPAddress.Parse(MainServerIP), MainServerPort);
                    SendWithACK(bm.GetBytes(), _remoteEndPoint);

                    while (true)
                    {
                        byte[] bytes = _client.Receive(ref _remoteEndPoint);
                        lock (ACKPacketsLock)
                            if (ACKPackets.Any(x => x.EP.Equals(_remoteEndPoint) && x.bytes.SequenceEqual(bytes)))
                            {
                                ACKPackets.RemoveAll(x => x.EP.Equals(_remoteEndPoint) && x.bytes.SequenceEqual(bytes));
                                continue;
                            }

                        bm.SetBytes(bytes);
                        int packetId = bm.GetPacketId();
                        int puid = bm.GetInt();

                        lock (ACKPacketIdsLock)
                            if (ACKPacketIds.Any(x => x.Equals(puid)))
                                continue;

                        _client.Send(bytes, _remoteEndPoint);

                        switch (packetId)
                        {
                            //Redirect to specific server
                            case 0x00:
                                string srvIP = bm.GetString();
                                int srvPort = bm.GetInt();
                                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(srvIP), srvPort);
                                bm.SetPacketId(0x00);
                                if (_isAuthenticated)
                                    bm.AddInt(_clientID);
                                SendWithACK(bm.GetBytes(), _remoteEndPoint);
                                break;
                            //Authentication shit
                            case 0x01:
                                _isAuthenticated = true;

                                _clientID = bm.GetInt();

                                bm.SetPacketId(0x01);
                                bm.AddInt(_clientID);
                                Console.WriteLine($"Running as {_clientID}");
                                SendWithACK(bm.GetBytes(), _remoteEndPoint);

                                //Send other crap
                                break;
                            //Authentication failed
                            case 0xff:
                                string reason = bm.GetString();
                                _isAuthenticated = false;
                                Console.WriteLine($"Disconnected: {reason}");
                                _client.Dispose();
                                Environment.Exit(0xff);
                                break;
                            //Get required data for p2p
                            case 0x02:
                                _connectedServerID = bm.GetLong();
                                int clientCount = bm.GetInt();

                                for (int i = 0; i < clientCount; i++)
                                {
                                    Peer peer = new Peer()
                                    {
                                        ClientID = bm.GetInt(),
                                        ServerID = bm.GetLong()
                                    };
                                    _peers.Add(peer);

                                    Console.WriteLine($"Added new peer {peer.ClientID}:{peer.ServerID}");
                                }

                                break;
                            case 0x05:
                                Console.WriteLine(bm.GetString());
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }


        public void poopoo()
        {
            BufferManager bm = new BufferManager();
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    bm.SetPacketId(0x05);
                    bm.AddString(Console.ReadKey().KeyChar.ToString());
                    SendData(bm.GetBytes());
                }

                Thread.Sleep(1);
            }
        }

        public void SendData(byte[] bytes)
        {
            BroadcastWithACK(bytes);
        }

        #region ACK / Reliable UDP
        private void BroadcastWithACK(byte[] bytes, bool includeSelf = false)
        {
            BufferManager bm = new BufferManager();
            
            for (int i = 0; i < _peers.Count; i++)
            {
                if (_peers[i].ClientID != _clientID)
                {
                    //int puid = GetPacketID();
                    //bytes = BufferManager.SetPacketUid(puid, bytes);

                    bm.SetPacketId(0x03);
                    bm.AddInt(_peers[i].ClientID);
                    bm.AddLong(_peers[i].ServerID);
                    bm.InsertBytes(bytes);

                    int puid = GetPacketID();
                    byte[] redirectBytes = BufferManager.SetPacketUid(puid, bm.GetBytes());

                    Packet p = new Packet()
                    {
                        bytes = redirectBytes,
                        EP = _remoteEndPoint,
                        PacketUID = puid
                    };

                    lock (ACKPacketsLock)
                        ACKPackets.Add(p);
                    lock (ACKPacketIdsLock)
                        ACKPacketIds.Add(puid);
                    _client.Send(redirectBytes, _remoteEndPoint);
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
