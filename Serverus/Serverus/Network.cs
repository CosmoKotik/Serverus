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
        private const int listenPort = 11000;

        public int _MaxPlayers { get; set; } = 6;
        public int _CurrentPlayers { get; set; } = 0;
        public int _ServerTicks { get; set; } = 20;

        private List<IPAddress> _clientsIP = new List<IPAddress>();
        private List<IPEndPoint> _clientsEP = new List<IPEndPoint>();
        private List<int> _clientPing = new List<int>();
        private List<Thread> _threads = new List<Thread>();


        public UdpClient listener = new UdpClient(listenPort);

        public void StartServer()
        {
            //_clientsIP.Add(IPAddress.Parse("0.0.0.0"));
            try
            {
                Thread timeout_T = new Thread(() => TimeoutService());
                timeout_T.Start();
                Console.WriteLine("Server listening on " + "");
                while (true)
                {
                    IPEndPoint _defaultEP = new IPEndPoint(IPAddress.Parse("192.168.1.12"), 11000);
                    byte[] bytes = listener.Receive(ref _defaultEP);
                    if (!_clientsIP.Contains(_defaultEP.Address) && _CurrentPlayers != _MaxPlayers)
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
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
            Console.WriteLine("Server crashed ;-;");
        }

        private void NetworkHandler(int client_index)
        {
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
                    byte[] bytes = listener.Receive(ref ep);
                    Console.WriteLine($"Received data from {ep} with ping of:{ping}, data :");
                    Console.WriteLine($"{Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                    for (int i = 0; i < _clientsEP.Count; i++)
                    {
                        listener.Send(bytes, bytes.Length, _clientsEP[i]);
                        Console.WriteLine(i);
                    }
                    ping = Environment.TickCount - lastReceivedTick;
                    lastReceivedTick = Environment.TickCount;
                    _clientPing[c_idx] = lastReceivedTick;
                }
                catch 
                {
                    Console.WriteLine($"Client {ep.Address} error");
                    return;
                }

                if ((1000 / _ServerTicks) > lastReceivedTick - startTick)
                {
                    Thread.Sleep(ping);
                }
            }
            _CurrentPlayers--;
            Thread.CurrentThread.Interrupt();
        }

        private void TimeoutService()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                for (int i = 0; i < _clientPing.Count; i++)
                {
                    int ping = Environment.TickCount - _clientPing[i];
                    if (ping >= 1000)
                    {
                        Console.WriteLine(_clientsIP.ToString() + " has been kicked for: Timeout");
                        _threads[i].Interrupt();
                        _clientsEP.RemoveAt(i);
                        _clientsIP.RemoveAt(i);
                        _CurrentPlayers--;
                    }
                }
            }
            Thread.CurrentThread.Interrupt();
        }
    }
}
