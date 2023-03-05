using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.Diagnostics.Contracts;

namespace ServerusServer.Core
{
    public class Network
    {
        private const int listenPort = 11000;

        private List<Peer> _peers = new List<Peer>();
        private List<IPEndPoint> _peersIps = new List<IPEndPoint>();

        private IPEndPoint groupEP;

        private List<byte[]> _broadcastBytes = new List<byte[]>();

        //private List<IPEndPoint> _peers = new List<IPEndPoint>();
        //private List<Thread> _peersThreads = new List<Thread>();

        private UdpClient _listener;

        public async Task StartServerAsync()
        {

            IPEndPoint bind = new IPEndPoint(IPAddress.Parse("10.0.1.3"), 11000);
            using (_listener = new UdpClient(bind))
            {
                _listener.MulticastLoopback = true;
                _listener.AllowNatTraversal(true);
                //_listener.ExclusiveAddressUse = false;
                //_listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                groupEP = new IPEndPoint(IPAddress.Any, listenPort);

                BufferManager bm = new BufferManager();
                try
                {
                    Thread tb = new Thread(() => BroadcastHandler());
                    tb.Start();

                    Console.WriteLine("Server started");
                    while (true)
                    {
                        //var receivedBuffer = await _listener.ReceiveAsync();
                        byte[] bytes = new byte[0];
                        if (_listener.Available > 0)
                            lock(_listener)
                                bytes = _listener.Receive(ref groupEP);

                        if (bytes.Length > 0)
                        {
                            if (_peersIps.Contains(groupEP))
                            {
                                int peerIndex = _peersIps.FindIndex(x => x.Equals(groupEP));
                                bm.SetBytes(bytes);

                                switch (bm.GetPacketId())
                                {
                                    case 0x05:
                                        for (int i = 0; i < _peers.Count; i++)
                                            lock (_peers[peerIndex].ImportantByteQueue)
                                                _peers[i].ImportantByteQueue.Add(bytes);
                                        break;
                                }
                                lock (_peers[peerIndex].ByteQueue)
                                {
                                    _peers[peerIndex].ByteQueue.Add(bytes);
                                    _peers[peerIndex].ReceivedData = true;
                                }
                            }
                            else
                            {
                                bm.SetBytes(bytes);
                                if (bm.GetPacketId().Equals(0x00))
                                {
                                    Peer p = new Peer();
                                    Thread t = new Thread(() => PeerHandler(p));
                                    p.Id = new Random().Next(int.MinValue, int.MaxValue);
                                    p.IsConnected = true;
                                    p.EndPoint = groupEP;
                                    p.PeerThread = t;
                                    p.ByteQueue = new List<byte[]>();
                                    p.ImportantByteQueue = new List<byte[]>();

                                    _peers.Add(p);
                                    _peersIps.Add(groupEP);
                                    t.Start();
                                    //Console.WriteLine($"peer {groupEP.Address}:{groupEP.Port} has connected");
                                }
                            }
                        }
                        //byte[] bytes = receivedBuffer.Buffer;

                        //byte[] data = Encoding.UTF8.GetBytes("cock");

                        //_listener.Send(data);
                        //_listener.Send(0x01, 1, groupEP);

                        //Console.WriteLine($"Received broadcast from {groupEP} :");
                        //Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");

                        //Thread.Sleep(1);
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    _listener.Close();
                }
            }
        }

        private void BroadcastHandler()
        {
            while (true)
            {
                if (_broadcastBytes.Count > 0)
                {
                    for (int i = 0; i < _peers.Count; i++)
                    {
                        lock(_broadcastBytes)
                            lock(_listener)
                                _listener.Send(_broadcastBytes[0], _broadcastBytes[0].Length, _peers[i].EndPoint);
                    }
                    lock (_broadcastBytes)
                        _broadcastBytes.RemoveAt(0);
                }
            }
        }

        private void Broadcast(byte[] buffer, IPEndPoint selfId, bool includeSelf = false)
        {
            for (int i = 0; i < _peers.Count; i++)
            {
                if (_peers[i].EndPoint != selfId)
                {
                    lock(_peers[i].ImportantByteQueue)
                        _peers[i].ImportantByteQueue.Add(buffer);
                }
                    //Send(_peers[i].EndPoint, buffer);
            }
        }

        private void Send(IPEndPoint ep, byte[] buffer)
        {
            _listener.Send(buffer, buffer.Length, ep);
        }

        private void PeerHandler(Peer peer)
        {
            Console.WriteLine($"peer {peer.EndPoint.Address}:{peer.EndPoint.Port} has connected");

            try
            {
                //IPEndPoint groupEP = new IPEndPoint(peer.EndPoint.Address, peer.EndPoint.Port);
                //UdpClient listener = new UdpClient(peer.EndPoint);

                BufferManager bm = new BufferManager();

                bm.SetPacketId(0x01);
                //bm.AddLong(RandomUID());
                Send(peer.EndPoint, bm.GetBytes());

                while (peer.IsConnected)
                {
                    byte[] bytes = new byte[0];
                    //bool hasReceivedData = false;
                    while (peer.ByteQueue.Count < 1) { Thread.Sleep(1); }
                    if (peer.ImportantByteQueue.Count > 0)
                    {
                        lock (peer.ImportantByteQueue)
                        {
                            bytes = (byte[])peer.ImportantByteQueue[0].Clone();
                            peer.ImportantByteQueue.RemoveRange(0, 1);
                        }
                    }
                    else
                    {
                        lock (peer.ByteQueue)
                        {
                            bytes = (byte[])peer.ByteQueue[0].Clone();
                            peer.ByteQueue.RemoveRange(0, 1);
                        }
                    }
                    //if (peer.ByteQueue.Count < 1)
                    //    peer.ReceivedData = false;
                    //byte[] bytes = _listener.Receive(ref groupEP);
                    //Console.WriteLine(bytes[0]);

                    if (bytes.Length > 0)
                    {
                        bool canSend = true;
                        bm.SetBytes(bytes);
                        int packetID = bm.GetPacketId();
                        //long packetUID = bm.GetLong();

                        switch (packetID)
                        {
                            case 0x00:
                                bm.SetPacketId(0x01);
                                //bm.AddLong(RandomUID());
                                break;
                            case 0x02:
                                bm.SetPacketId(0x04);
                                //bm.AddLong(RandomUID());
                                string allPeers = "";
                                foreach (IPEndPoint p in _peersIps)
                                {
                                    if (p != peer.EndPoint)
                                        allPeers += $"{p.Address}:{p.Port},";
                                }
                                bm.AddString(allPeers);
                                //bm.AddString("hello " + peer.EndPoint.Port);
                                break;
                            case 0x03:
                                Console.WriteLine(bm.GetString());
                                bm.SetPacketId(0x03);
                                //bm.AddLong(RandomUID());
                                bm.AddString("hello " + peer.EndPoint.Port);
                                break;
                            case 0x04:
                                
                                break;
                            case 0x05:
                                //canSend = false;

                                bm.SetPacketId(0x06);
                                //bm.AddLong(RandomUID());
                                bm.AddString($"{peer.EndPoint.Address}:{peer.EndPoint.Port}");
                                byte[] peerBytes = (byte[])bm.GetBytes().Clone();
                                //Broadcast(peerBytes, peer.EndPoint);
                                _broadcastBytes.Add(peerBytes);
                                bm.SetPacketId(0x03);
                                //bm.AddLong(RandomUID());
                                bm.AddString("hello " + peer.EndPoint.Port);
                                break;
                        }

                        if (canSend)
                            Send(peer.EndPoint, bm.GetBytes());
                    }

                    //Console.WriteLine(Encoding.ASCII.GetString(bytes));
                    //Thread.Sleep(33);
                }
            }
            catch(Exception ex) { peer.PeerThread.Interrupt(); }
        }

        private long RandomUID()
        {
            Random rand = new Random();
            return rand.NextInt64();
        }
    }
}
