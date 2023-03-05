using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Collections;

namespace ServerusClient.Core
{
    internal class Network
    {
        private List<IPEndPoint> _peers = new List<IPEndPoint>();
        private List<Packet> _sendedPackets = new List<Packet>();
        private List<Packet> _receivedSendedPackets = new List<Packet>();

        private List<Packet> _receivedPackets = new List<Packet>();

        private List<byte[]> _totalReceivedBytes = new List<byte[]>();
        private List<byte[]> _receivedBytes = new List<byte[]>();
        private List<byte[]> _sendedBytes = new List<byte[]>();

        public async Task ConnectAsync()
        {
            string ip = "10.0.1.3";
            //string ip = "72.140.210.214";
            int port = new Random().Next(40000, 60000);
            //listener.ExclusiveAddressUse = false;
            //listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);
            IPEndPoint self = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), port);
            IPEndPoint server = new IPEndPoint(IPAddress.Parse("10.0.1.3"), 11000);
            using (UdpClient listener = new UdpClient(port))
            {
                listener.AllowNatTraversal(true);
                listener.MulticastLoopback = true;
                try
                {
                    //AppDomain.CurrentDomain.ProcessExit
                    Thread loopbackThread = new Thread(() => HandleLoopBack(listener));
                    //loopbackThread.Start();

                    Console.WriteLine($"Started {port}");
                    //listener.Connect(groupEP);
                    BufferManager bm = new BufferManager();
                    bm.SetPacketId(0x00);
                    //bm.AddLong(RandomUID());
                    listener.Send(bm.GetBytes(), ip, 11000);
                    string[] ipport;
                    IPEndPoint peer;
                    while (true)
                    {
                        bool canBroadcast = true;
                        
                        byte[] bytes = new byte[0];
                        if (listener.Available > 0)
                            bytes = listener.Receive(ref groupEP);

                        //var receivedBuffer = await listener.ReceiveAsync();
                        //bytes = receivedBuffer.Buffer;

                        if (bytes.Length > 0)
                        {
                            bm.SetBytes(bytes);
                            int pid = bm.GetPacketId();
                            //long puid = bm.GetLong();
                            Packet packet = new Packet()
                            {
                                PacketID = pid,
                                bytes = bytes,
                                ep = groupEP
                            };

                            /*if (groupEP.Port != 11000)
                            {
                                if (!_sendedPackets.Exists(x => x.uid == puid))
                                {
                                    listener.Send(packet.bytes, packet.ep);
                                    _sendedPackets.Add(packet);
                                }
                                else
                                {
                                    _sendedPackets.Remove(packet);
                                    continue;
                                }
                            }*/

                            if (groupEP.Port != 11000)
                            {
                                /*if (_sendedPackets.Exists(x => x.uid == puid))
                                {
                                    _sendedPackets.Remove(packet);
                                    continue;
                                }
                                else
                                {
                                    if (!_receivedSendedPackets.Exists(x => x.uid == puid))
                                    {
                                        _receivedSendedPackets.Add(packet);
                                        lock (_receivedQueue)
                                            _receivedQueue.Add(packet);
                                    }
                                    //listener.Send(packet.bytes, packet.ep);
                                }*/
                                /*lock (_receivedPackets)
                                    _receivedPackets.Add(packet);*/

                                /*lock (_sendedBytes)
                                    if (_sendedBytes.Any(x => x.SequenceEqual(bytes)))
                                    {
                                        _sendedBytes.Remove(bytes);
                                        continue;
                                        //_receivedBytes.RemoveAt(0);
                                    }
                                    else
                                    {
                                        listener.Send(bytes, groupEP);
                                        //_receivedBytes.RemoveAt(0);
                                    }*/
                                //_receivedQueue.Add(packet);

                                lock (_sendedPackets)
                                    if (_sendedPackets.Any(x => x.bytes.SequenceEqual(bytes)))
                                    {
                                        _sendedPackets.Remove(packet);
                                        continue;
                                    }
                                    else
                                    {
                                        for (int i = 0; i < _sendedPackets.Count; i++)
                                            listener.Send(_sendedPackets[i].bytes, _sendedPackets[i].ep);
                                        //_sendedPackets.Add(packet);
                                        //if (!_totalReceivedBytes.Any(x => x.SequenceEqual(bytes)))
                                        //    listener.Send(bytes, groupEP);
                                    }
                                
                                _totalReceivedBytes.Add(bytes);
                            }

                            /*lock (_sendedPackets)
                                //if (_sendedPackets.Exists(x => x.bytes == bytes))
                                if (_sendedPackets.Contains(packet))
                                {
                                    _sendedPackets.Remove(packet);
                                    continue;
                                }
                                else
                                {
                                    if (groupEP.Port != 11000)
                                        if (!_receivedSendedPackets.Contains(packet))
                                        {
                                            _receivedSendedPackets.Add(packet);
                                            _sendedPackets.Add(packet);
                                        }
                                }*/

                            switch (packet.PacketID)
                            {
                                case 0x01:
                                    bm.SetPacketId(0x02);
                                    //bm.AddLong(RandomUID());
                                    break;
                                case 0x03:
                                    Console.WriteLine(bm.GetString());
                                    //listener.Send(packet.bytes, packet.ep);
                                    //bm.SetPacketId(0x03);
                                    //bm.AddString(groupEP.Port + " Hiiiiii " + port);
                                    //bm.AddString(self.ToString() + " : " + Console.ReadLine());
                                    break;
                                case 0x04:
                                    string[] splitPeers = bm.GetString().Split(',');
                                    for (int i = 0; i < splitPeers.Length; i++)
                                    {
                                        if (splitPeers[i].Length > 1)
                                        {
                                            ipport = splitPeers[i].Split(':');
                                            peer = new IPEndPoint(IPAddress.Parse(ipport[0]), int.Parse(ipport[1]));
                                            if (!_peers.Contains(peer) && peer.Port != port)
                                            {
                                                _peers.Add(peer);
                                                Thread t = new Thread(() => HandlePeer(listener, peer, port));
                                                t.Start();
                                                Console.WriteLine(peer);
                                            }
                                        }
                                    }
                                    bm.SetPacketId(0x05);
                                    //bm.AddLong(RandomUID());
                                    break;
                                case 0x06:
                                    canBroadcast = false;

                                    ipport = bm.GetString().Split(':');
                                    peer = new IPEndPoint(IPAddress.Parse(ipport[0]), int.Parse(ipport[1]));
                                    if (!_peers.Contains(peer) && peer.Port != port)
                                    {
                                        _peers.Add(peer);
                                        Thread t = new Thread(() => HandlePeer(listener, peer, port));
                                        t.Start();
                                        Console.WriteLine(peer);
                                    }
                                    bm.SetPacketId(0x03);
                                    //bm.AddLong(RandomUID());
                                    bm.AddString(groupEP.Port + " Hiiiiii " + port);
                                    break;
                            }


                            /*for (int i = 0; i < _peers.Count; i++)
                            {
                                bm.SetPacketId(0x03);
                                bm.AddString(groupEP.Port + " Hiiiiii " + port);
                                listener.Send(bm.GetBytes(), _peers[i]);
                            }*/

                            listener.Send(bm.GetBytes(), ip, 11000);
                        }

                        if (Console.KeyAvailable)
                        {
                            bm.SetPacketId(0x03);
                            //bm.AddLong(RandomUID());
                            bm.AddString(port + " : " + Console.ReadKey(true).KeyChar);
                            listener.Send(bm.GetBytes(), ip, 11000);
                        }

                        //listener.Send(bytes);

                        //Console.WriteLine($"Received broadcast from {groupEP} :");
                        //Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                        //Thread.Sleep(33);
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    listener.Close();
                }
            }
        }

        private void HandlePeer(UdpClient listener, IPEndPoint groupEP, int port)
        {
            BufferManager bm = new BufferManager();
            while (true)
            {
                //bm.SetPacketId(0x03);
                //bm.AddString(groupEP.Port + " Hiiiiii " + port);

                /*lock (_receivedQueue)
                {
                    if (_receivedQueue.Count > 0)
                    {
                        if (_sendedPackets.Exists(x => x.bytes == _receivedQueue[0].bytes))
                        {
                            _sendedPackets.Remove(_receivedQueue[0]);
                            _receivedQueue.RemoveAt(0);
                        }
                        else
                        {
                            listener.Send(_receivedQueue[0].bytes, _receivedQueue[0].ep);
                            _receivedQueue.RemoveAt(0);
                        }
                    }
                }*/

                if (Console.KeyAvailable)
                {
                    //long packetUid = RandomUID();
                    byte packetId = 0x03;
                    bm.SetPacketId(packetId);
                    //bm.AddLong(packetUid);
                    bm.AddString(port + " : " + Console.ReadKey(true).KeyChar);
                    
                    Packet p = new Packet();
                    p.PacketID = packetId;
                    //p.uid = packetUid;
                    p.bytes = bm.GetBytes();
                    p.ep = groupEP;

                    _sendedPackets.Add(p);
                    _sendedBytes.Add(bm.GetBytes());
                    listener.Send(bm.GetBytes(), groupEP);
                    //lock(_sendedPackets)
                    //    _sendedPackets.Add(p);
                }

                //Console.WriteLine($"Sending to: {groupEP}");

                //Thread.Sleep(33);
            }
        }

        public void HandleLoopBack(UdpClient listener)
        {
            while (true)
            {
                Packet[] packets = new Packet[0];

                lock (_sendedPackets)
                    packets = _sendedPackets.ToArray();

                if (packets.Length > 0)
                {
                    for (int i = 0; i < packets.Length; i++)
                    {
                            listener.Send(packets[i].bytes, packets[i].ep);
                    }
                }

                Thread.Sleep(250);
            }
        }

        private long RandomUID()
        {
            Random rand = new Random();
            return rand.NextInt64(0, long.MaxValue);
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
