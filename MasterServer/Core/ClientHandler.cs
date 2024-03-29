﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer.Core
{
    internal class ClientHandler
    {
        private Network _network = default!;
        public ClientHandler(Network network)
        {
            this._network = network;
        }

        public void NetworkHandler(TcpClient client, Server srv)
        {
            try
            {
                BufferManager bm = new BufferManager();
                NetworkStream stream = client.GetStream();

                bm.SetPacketId(0x00);
                bm.AddLong(srv.ServerID);
                bm.AddInt((int)srv.SrvType);

                stream.Write(bm.GetBytes(), 0, bm.GetBytes().Length);
                //client.Client.Send(bm.GetBytes());
                stream.Flush();
                //Thread.Sleep(10);

                //stream.Flush();

                Console.WriteLine($"New {srv.SrvType}:{srv.ServerID} Server has connected successfully");

                byte[] bytes = new byte[1024];
                int size;

                long serverId;
                int clientId;
                Client peer;

                while (client.Connected)
                {
                    while ((size = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        bm.SetBytes(bytes);

                        switch (bm.GetPacketId())
                        {
                            case 0x00:
                                long id = bm.GetLong();
                                //_network.Servers.Find(x => x.ServerID.Equals(id)).IP = bm.GetString();
                                //_network.Servers.Find(x => x.ServerID.Equals(id)).Port = bm.GetInt();

                                srv.LocalIP = bm.GetString();
                                srv.PublicIP = bm.GetString();
                                srv.Port = bm.GetInt();
                                srv.UdpPort = bm.GetInt();
                                srv.MaxConnections = bm.GetInt();

                                //Console.WriteLine(srv.Port);

                                lock (_network.Servers)
                                {
                                    for (int i = 0; i < _network.Servers.Count; i++)
                                    {
                                        bm.SetPacketId(0x01);
                                        bm.AddLong(_network.Servers[i].ServerID);
                                        bm.AddInt((int)_network.Servers[i].SrvType);
                                        bm.AddString(_network.Servers[i].LocalIP);
                                        bm.AddString(_network.Servers[i].PublicIP);
                                        
                                        if (_network.Servers[i].IsServerLocal && srv.IsServerLocal)         //Checks if new server is local with other server
                                            bm.AddBool(true);
                                        else
                                            bm.AddBool(false);

                                        bm.AddInt(_network.Servers[i].Port);
                                        bm.AddInt(_network.Servers[i].UdpPort);
                                        bm.AddInt(_network.Servers[i].MaxConnections);

                                        stream.Write(bm.GetBytes(), 0, bm.GetBytes().Length);
                                        stream.Flush();
                                        //client.Client.Send(bm.GetBytes());

                                        bm.SetPacketId(0x01);
                                        bm.AddLong(srv.ServerID);
                                        bm.AddInt((int)srv.SrvType);
                                        bm.AddString(srv.LocalIP);
                                        bm.AddString(srv.PublicIP);

                                        if (_network.Servers[i].IsServerLocal && srv.IsServerLocal)         //Checks if new server is local with other server
                                            bm.AddBool(true);
                                        else
                                            bm.AddBool(false);
                                        
                                        bm.AddInt(srv.Port);
                                        bm.AddInt(srv.UdpPort);
                                        bm.AddInt(srv.MaxConnections);
                                        lock (_network.Servers[i].Client)
                                        {
                                            _network.Servers[i].Client.GetStream().Write(bm.GetBytes(), 0, bm.GetBytes().Length);
                                            _network.Servers[i].Client.GetStream().Flush();
                                        }
                                        //_network.Servers[i].Client.Client.Send(bm.GetBytes());
                                        
                                        Thread.Sleep(100);
                                    }
                                    _network.Servers.Add(srv);
                                }

                                break;
                            case 0x02:
                                //Transfer current clients
                                bm.SetPacketId(0x02);
                                lock (_network.Udp)
                                {
                                    bm.AddInt(_network.Udp.ClientPeers.Count);
                                    for (int i = 0; i < _network.Udp.ClientPeers.Count; i++)
                                    {
                                        peer = _network.Udp.ClientPeers[i];
                                        bm.AddInt(peer.ClientID);
                                        bm.AddLong(peer.ServerID);
                                        bm.AddString(peer.ClientEndPoint.Address.ToString());
                                        bm.AddInt(peer.ClientEndPoint.Port);
                                    }
                                    stream.Write(bm.GetBytes(), 0, bm.GetBytes().Length);
                                }
                                break;
                            case 0x05:
                                serverId = bm.GetLong();
                                int cc = bm.GetInt();
                                _network.Servers.Find(x => x.ServerID.Equals(serverId))!.CurrentConnections = cc;


                                break;
                            case 0x06:
                                serverId = bm.GetLong();
                                clientId = bm.GetInt();

                                peer = new Client()
                                {
                                    ServerID = serverId,
                                    ClientID = clientId,
                                    ClientEndPoint = new IPEndPoint(IPAddress.Parse(bm.GetString()), bm.GetInt())
                                };

                                _network.Udp.ClientPeers.Add(peer);

                                break;
                        }
                    }

                    //Thread.Sleep(1);
                }

                _network.Servers.Remove(srv);
                Thread.CurrentThread.Interrupt();
            }
            catch
            {
                Console.WriteLine($"Server {srv.ServerID} Has Lost connection");

                client.Dispose();
                _network.Servers.Remove(srv);
                Thread.CurrentThread.Interrupt();
            }
        }
    }
}
