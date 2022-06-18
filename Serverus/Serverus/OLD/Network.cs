using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

namespace Git_Git_Server
{
    public class Network
    {
        private const int listenPort = 11001;

        public int _MaxPlayers { get; set; } = 6;
        public int _CurrentPlayers { get; set; } = 0;
        public int _ServerTicks { get; set; } = 20;

        private List<IPAddress> _clientsIP = new List<IPAddress>();
        private List<IPEndPoint> _clientsEP = new List<IPEndPoint>();
        private List<int> _clientPing = new List<int>();
        private List<Thread> _threads = new List<Thread>();


        public UdpClient listener = new UdpClient(listenPort);

        public void StartServer(string ip)
        {
            Thread timeout_T = new Thread(() => TimeoutService());
            //_clientsIP.Add(IPAddress.Parse("0.0.0.0"));
            try
            {
                timeout_T.Start();
                Console.WriteLine("Server listening on " + "");
                //listener.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));

                listener.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));

                while (true)
                {
                    IPEndPoint _defaultEP = new IPEndPoint(IPAddress.Parse(ip), listenPort);

                    if (listener.Available != 0)
                    { 
                        byte[] bytes = listener.Receive(ref _defaultEP);

                        if (!_clientsEP.Contains(_defaultEP) && _CurrentPlayers != _MaxPlayers)
                        {
                            Console.WriteLine($"Request from {_defaultEP} for: {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");

                            Console.WriteLine($"Registring player: {_defaultEP.Address}");

                            _clientsIP.Add(_defaultEP.Address);
                            _clientsEP.Add(_defaultEP);
                            _clientPing.Add(Environment.TickCount);

                            Console.WriteLine($"Trying to connect {_defaultEP}");
                            _CurrentPlayers++;
                            Thread t = new Thread(() => NetworkHandler(_CurrentPlayers));
                            _threads.Add(t);
                            t.Start();
                        }
                        
                    }

                    Thread.Sleep(1);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                timeout_T.Interrupt();
                listener.Dispose();
                listener.Close();
            }
            Console.WriteLine("Server crashed ;-;");
        }

        private void NetworkHandler(int client_index)
        {
            //listener.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));

            Console.WriteLine($"Player connected");
            int c_idx = client_index - 1;

            int startTick = Environment.TickCount;
            int lastReceivedTick = Environment.TickCount;
            int ping = 0;

            while (Thread.CurrentThread.IsAlive)
            {
                IPEndPoint ep = new IPEndPoint(_clientsIP[c_idx], listenPort);
                if (ep.Address != _clientsIP[c_idx])
                    return;

                try
                {
                    if (listener.Client == null)
                        return;

                    byte[] bytes = listener.Receive(ref ep);

                    //Console.WriteLine($"Received data from {ep} with ping of:{ping}, data : {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");

                    listener.Send(bytes, bytes.Length, "255.255.255.255", listenPort);

                    if (Encoding.ASCII.GetString(bytes, 0, bytes.Length) != " ")
                    {
                        /*for (int i = 0; i < _clientsEP.Count; i++)
                        {
                            //Console.WriteLine(_clientsEP.Count);

                            //if (_clientsEP[i] != ep)
                                listener.Send(bytes, bytes.Length, _clientsEP[i]);

                            Console.WriteLine(i);
                        }*/

                        /*foreach (IPEndPoint endP in _clientsEP)
                        {
                            listener.Send(bytes, bytes.Length, endP);
                        }*/
                        //listener.Send(bytes, bytes.Length, "255.255.255.0", listenPort);
                    }


                    ping = Environment.TickCount - lastReceivedTick;
                    lastReceivedTick = Environment.TickCount;
                     _clientPing[c_idx] = lastReceivedTick;
                }
                catch (SocketException e)
                {
                    Console.WriteLine($"Client {ep.Address} error: " + e);
                    return;
                }

                if ((1000 / _ServerTicks) > lastReceivedTick - startTick)
                {
                    Thread.Sleep(ping);
                }
            }
            _CurrentPlayers--;
            Console.WriteLine("Client disconnected");
            Thread.CurrentThread.Interrupt();
        }

        private void TimeoutService()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                for (int i = 0; i < _clientPing.Count; i++)
                {
                    int ping = Environment.TickCount - _clientPing[i];
                    if (ping >= 8000)
                    {
                        Console.WriteLine(_clientsIP.ToString() + " has been kicked for: Timeout");
                        _clientsEP.RemoveAt(i);
                        _clientsIP.RemoveAt(i);
                        _clientPing.RemoveAt(i);
                        _threads[i].Interrupt();
                        _CurrentPlayers--;
                    }
                }
            }
            Thread.CurrentThread.Interrupt();
        }
    }
}
