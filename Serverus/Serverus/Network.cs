using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using UnityEngine;
using Autofactory.CommandTerminal;
using System.Linq;
using static NetAction;

public class Network : MonoBehaviour
{
    public List<NetObject> NetObjects = new List<NetObject>();


    public int _MaxPlayers { get; set; } = 10;
    public int _CurrentPlayers { get; set; } = 0;
    public int _ServerTicks { get; set; } = 20;

    private List<IPAddress> _clientsIP = new List<IPAddress>();
    private List<int> _clientsPort = new List<int>();
    private List<IPEndPoint> _clientsEP = new List<IPEndPoint>();
    private List<NetClass> _clientsData = new List<NetClass>();
    private List<int> _clientsId = new List<int>();

    private Dictionary<int, int> _sendQueue = new Dictionary<int, int>();
    private Dictionary<int, NetChecker> _checkQueue = new Dictionary<int, NetChecker>();

    private List<string> _recvData = new List<string>();

    private List<int> _clientPing = new List<int>();
    private List<Thread> _threads = new List<Thread>();

    private int listenPort = 11000;
    private string listenIp = "10.0.1.3";

    private Thread _mainThread;
    private Thread _reSenderThread;
    private Thread timeout_T;

    public UdpClient listener;

    public BlockManager BlockList;

    //private string _recvData;

    public Terminal trm;

    private void Start()
    {
        //StartServer("127.0.0.1");
        //Terminal.Log("{0}", "asdasdasd");
        Terminal.Shell.AddCommand("StartServer", StartServerCommand, 0, 2, "Start the server");
        Terminal.Shell.AddCommand("Stop", StopServerCommand, 0, 2, "Stop the server");
        Terminal.Shell.AddCommand("Restart", RestartServerCommand, 0, 2, "Restart the server");

        this.BlockList = this.GetComponent<BlockManager>();
    }

    private void Update()
    {
        if (_recvData.Count != 0)
        {
            string data;
            lock (_recvData)
            {
                data = _recvData[0];
                _recvData.RemoveAt(0);
                //Debug.Log(_recvData.Count.ToString());
            }
    
            //_dataIndex++;
    
    
            if (data != "")
            {
                GameObject obj;

                NetAction na;
                na = JsonUtility.FromJson<NetAction>(data);
                //na = JsonConvert.DeserializeObject<NetAction>(_recvData);
    
    
                switch (na.CurrentAction)
                {
                    case Actions.Spawn:
                        if (!transform.Find(na.Id.ToString()))
                        {
                            NetSpawning ns = JsonUtility.FromJson<NetSpawning>(na.ActionObject);
                            //NetSpawning ns = JsonConvert.DeserializeObject<NetSpawning>(na.ActionObject);
    
                            obj = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                            obj.transform.parent = this.transform;
                            obj.name = ns.Id.ToString();
                            obj.transform.position = ns.Position;
                            obj.transform.eulerAngles = ns.Rotation;
                        }
                        break;
                    case Actions.Placing:
                        NetPlacing np = JsonUtility.FromJson<NetPlacing>(na.ActionObject);
                        //NetPlacing np = JsonConvert.DeserializeObject<NetPlacing>(na.ActionObject);
    
                        if (!transform.Find(na.Id.ToString()) && !np.IsDestroyed)
                        {
                            obj = Instantiate(BlockList.Blocks[np.Block]);
                            Destroy(obj.GetComponent<NetObject>());
                            obj.transform.parent = this.transform;
                            obj.name = np.Id.ToString();
                            obj.transform.position = np.Position;
                            obj.transform.eulerAngles = np.Rotation;
                        }
                        else if (transform.Find(na.Id.ToString()) && np.IsDestroyed)
                        {
                            obj = transform.Find(na.Id.ToString()).gameObject;
                            Destroy(obj);
                        }
    
                        break;
                    case Actions.Movement:
                        NetMovement nm = JsonUtility.FromJson<NetMovement>(na.ActionObject);
                        //NetMovement nm = JsonConvert.DeserializeObject<NetMovement>(na.ActionObject);
    
                        obj = this.transform.Find(nm.Id.ToString()).gameObject;
                        obj.transform.position = nm.Position;
                        obj.transform.eulerAngles = nm.Rotation;
    
                        break;
                }
            }
        }
    }

    private void StartServerCommand(CommandArg[] obj)
    {
        StartServer(listenIp, listenPort);
    }

    private void StopServerCommand(CommandArg[] obj)
    {
        timeout_T.Interrupt();
        _reSenderThread.Interrupt();
        listener.Dispose();
        listener.Close();
        Debug.Log("server closed");
        _mainThread.Interrupt();
    }

    private void RestartServerCommand(CommandArg[] obj)
    {
        timeout_T.Interrupt();
        _reSenderThread.Interrupt();
        listener.Dispose();
        listener.Close();
        Debug.Log("server closed");
        _mainThread.Interrupt();

        Thread.Sleep(1000);

        StartServer(listenIp, listenPort);
    }

    private void OnApplicationQuit()
    {
        timeout_T.Interrupt();
        _reSenderThread.Interrupt();
        listener.Dispose();
        listener.Close();
        Debug.Log("server closed");
        _mainThread.Interrupt();
    }

    public void StartServer(string ip, int port)
    {
        listener = new UdpClient();

        foreach (NetObject eye in GameObject.FindObjectsOfType(typeof(NetObject)))
        {
            NetObjects.Add(eye);
        }

        _mainThread = new Thread(() => MainThread(ip, port));
        _mainThread.Start();
    }

    private void MainThread(string ip, int port)
    {
        timeout_T = new Thread(() => TimeoutService());
        _reSenderThread = new Thread(() => ReSendQueueHandler());

        try
        {
            timeout_T.Start();
            _reSenderThread.Start();

            Debug.Log("Server listening on " + port);
            listener.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Client.EnableBroadcast = true;
            listener.EnableBroadcast = true;
            //listener.EnableBroadcast = true;
            while (true)
            {
                IPEndPoint _defaultEP = new IPEndPoint(IPAddress.Any, port);
                //Debug.Log("1");

                /*if (listener.Available != 0)
                {
                    byte[] bytes = listener.Receive(ref _defaultEP);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                    NetClass nc = JsonUtility.FromJson<NetClass>(data);


                    //if (!_clientsIP.Contains(_defaultEP.Address) && _CurrentPlayers != _MaxPlayers)
                    //if (!_clientsEP.Contains(_defaultEP) && _CurrentPlayers != _MaxPlayers)
                    
                    /*if (!_clientsPort.Contains(_defaultEP.Port))
                    {
                        RegisterPlayer(_defaultEP, bytes, nc.NetId);
                        Debug.Log("Port does not exist");
                    }
                    else if (!_clientsIP.Contains(_defaultEP.Address))
                    {
                        RegisterPlayer(_defaultEP, bytes, nc.NetId);
                        Debug.Log("Port exists, ip does not exist");
                    }*//*

                    if (!_clientsData.Contains(nc))
                    {
                        RegisterPlayer(_defaultEP, bytes, nc);
                        Debug.Log("id does not exists : " + nc.NetId);
                    }

                }*/

                if (listener.Available != 0)
                {
                    try
                    {
                        byte[] bytes = listener.Receive(ref _defaultEP);
                        string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                        //NetClass nc = JsonUtility.FromJson<NetClass>(data);
                        NetAction na = JsonUtility.FromJson<NetAction>(data);

                        if (!_clientsId.Contains(na.NetId))
                        {
                            RegisterPlayer(_defaultEP, bytes, na.NetId);

                            lock (_recvData)
                            {
                                _recvData.Add(Encoding.ASCII.GetString(bytes));
                                //Debug.Log(_recvData.Count.ToString());
                            }

                            //SendData(bytes, na, listener);

                            //Debug.Log("id does not exists : " + na.NetId);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                //Thread.Sleep(1);
        }
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
        finally
        {
            timeout_T.Interrupt();
            _reSenderThread.Interrupt();
            listener.Dispose();
            listener.Close();
            Debug.Log("server closed");
            Terminal.Log("{0}", "Server closed");
            _mainThread.Interrupt();
        }
        Debug.Log("Server crashed ;-;");
    }

    private void RegisterPlayer(IPEndPoint _defaultEP, byte[] bytes, int playerId)
    {
        if (_CurrentPlayers != _MaxPlayers)
        {
            Debug.Log($"Request from {_defaultEP} for: {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
            Terminal.Log("{0}", $"Request from {_defaultEP} for: {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");

            Debug.Log($"Registring player: {_defaultEP.Address}");
            Terminal.Log("{0}", $"Registring player: {_defaultEP.Address}");

            _clientsId.Add(playerId);
            _clientsIP.Add(_defaultEP.Address);
            _clientsPort.Add(_defaultEP.Port);
            _clientsEP.Add(_defaultEP);
            _clientPing.Add(Environment.TickCount);

            Debug.Log($"Trying to connect {_defaultEP}");
            Terminal.Log("{0}", $"Trying to connect {_defaultEP}");

            //listener.Send(bytes, bytes.Length, _defaultEP);

            _CurrentPlayers++;
            Thread t = new Thread(() => NetworkHandler(_CurrentPlayers, playerId));
            _threads.Add(t);
            t.Start();
        }
    }

    private void NetworkHandler(int client_index, int client_id)
    {
        UdpClient local_listener = listener;

        Debug.Log($"Player connected");
        Terminal.Log("{0}", "Player Connected");
        int c_idx = client_index - 1;

        int startTick = Environment.TickCount;
        int lastReceivedTick = Environment.TickCount;
        int ping = 0;

        //IPEndPoint ep = new IPEndPoint(_clientsIP[c_idx], listenPort);
        //IPEndPoint ep = new IPEndPoint(IPAddress.Any, _clientsPort[c_idx]);
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);
        IPEndPoint ep2 = new IPEndPoint(IPAddress.Broadcast, listenPort);

        //local_listener.Client.ReceiveTimeout = 6000;
        local_listener.Client.EnableBroadcast = true;
        local_listener.EnableBroadcast = true;

        while (Thread.CurrentThread.IsAlive)
        {
            try
            {
                /*if (ep.Address != _clientsIP[c_idx])
                    return;
                */
                /*if (client_id != _clientsId[c_idx])
                    return;

                if (local_listener.Client == null)
                    return;*/

                //Thread.Sleep(2);
                //byte[] bytes = local_listener.Receive(ref ep);

                IAsyncResult ir = local_listener.BeginReceive(null, null);
                byte[] bytes = local_listener.EndReceive(ir, ref ep);

                try
                {
                    //NetClass nc = JsonUtility.FromJson<NetClass>(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    NetAction na = JsonUtility.FromJson<NetAction>(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    if (na.CurrentAction != Actions.Verification)
                    {
                        lock (_recvData)
                        {
                            _recvData.Add(Encoding.ASCII.GetString(bytes));
                            //Debug.Log(_recvData.Count.ToString());
                        }
                    }
                    //Debug.Log(Encoding.ASCII.GetString(bytes, 0, bytes.Length));

                    if (na.CurrentAction == Actions.Verification)
                        lock (_checkQueue)
                        {
                            NetAction temp_na = JsonUtility.FromJson<NetAction>(Encoding.ASCII.GetString(_checkQueue[na.MessageId].BytesData, 0, _checkQueue[na.MessageId].BytesData.Length));
                            temp_na.CurrentAction = Actions.Verification;
                            Debug.Log("Receiverd");
                            SendData(Encoding.ASCII.GetBytes(JsonUtility.ToJson(temp_na)), _checkQueue[na.MessageId].NA, _checkQueue[na.MessageId].UPDConnection, true);
                            _checkQueue.Remove(na.MessageId);
                        }
                    //if (Encoding.ASCII.GetString(bytes, 0, bytes.Length) != " ")
                    //    for (int i = 0; i <= _clientsId.Count; i++)
                    //    {
                    //        IAsyncResult iar = local_listener.BeginSend(bytes, bytes.Length, _clientsEP[i], null, null);
                    //        //IAsyncResult iar = local_listener.BeginSend(bytes, bytes.Length, ep2, null, null);
                    //        local_listener.Client.EndSend(iar);
                    //    }
                    //local_listener.Send(bytes, bytes.Length, "255.255.255.255", listenPort);
                    //local_listener.BeginSend(bytes, bytes.Length, ep2, null, null);

                    //local_listener.Send(bytes, bytes.Length, "255.255.255.255", _clientsPort[c_idx]);

                    //----//udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                    if (na.CurrentAction != Actions.Verification)
                    {
                        SendData(bytes, na, local_listener);
                        SendData(bytes, na, local_listener);
                    }

                    if (na.NetId == _clientsId[c_idx])
                    {
                        //Debug.Log(c_idx + "  " + local_listener.Available);

                        //Debug.Log($"Received data from {ep} with ping of:{ping}, data : {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                        //Terminal.Log("{0}", $"Received data from {ep} with ping of:{ping}, data : {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");

                        ping = Environment.TickCount - lastReceivedTick;
                        lastReceivedTick = Environment.TickCount;
                        _clientPing[c_idx] = lastReceivedTick;
                    }
                    
                    //Thread.Sleep(1);
                }
                catch
                { 
                    
                }
            }
            catch (SocketException e)
            {
                Debug.Log($"Client {ep.Address} error: " + e);
                Terminal.Log("{0}", $"Client {ep.Address} error: " + e);
                return;
            }

            if ((1000 / _ServerTicks) > lastReceivedTick - startTick)
            {
                Thread.Sleep(ping);
            }

            //Thread.Sleep(1);
        }

        _clientsEP.RemoveAt(c_idx);
        _clientsIP.RemoveAt(c_idx);
        _clientsPort.RemoveAt(c_idx);
        //_clientsData.RemoveAt(i);
        _clientsId.RemoveAt(c_idx);
        _clientPing.RemoveAt(c_idx);
        _CurrentPlayers--;
        Debug.Log("Client disconnected");
        Terminal.Log("{0}", "Client disconnected");
        Thread.CurrentThread.Interrupt();
    }

    private void SendData(byte[] bytes, NetAction NA, UdpClient localClient, bool isQueueRechecker = false)
    {
        if (Encoding.ASCII.GetString(bytes, 0, bytes.Length) != " ")
            for (int i = 0; i < _clientsEP.Count; i++)
            {
                Debug.Log(Encoding.ASCII.GetString(bytes, 0, bytes.Length));

                if (_clientsId[i] != NA.NetId)
                {
                    string json = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    IAsyncResult iar = localClient.BeginSend(bytes, bytes.Length, _clientsEP[i], null, null);
                    //IAsyncResult iar = local_listener.BeginSend(bytes, bytes.Length, ep2, null, null);
                    localClient.Client.EndSend(iar);

                    //Debug.Log("resending3");
                    //_sendQueue.Add(NA.Id, NA.MessageId);
                    if (!isQueueRechecker && NA.CurrentAction != Actions.Movement)
                        lock (_checkQueue)
                        {
                            NetChecker NCheck = new NetChecker();
                            NCheck.SendTime = Environment.TickCount;
                            NCheck.NetId = NA.NetId;
                            NCheck.Id = NA.Id;
                            NCheck.EP = _clientsEP[i];
                            NCheck.BytesData = bytes;

                            NCheck.UPDConnection = localClient;
                            NCheck.NA = NA;
                            NCheck.Tries = 1;

                            _checkQueue.Add(NA.MessageId, NCheck);
                            //_checkQueue.Add(NA.MessageId, NCheck);
                            //_checkQueue.Add(NA.MessageId, NCheck);
                            //Debug.Log(_checkQueue.Count.ToString());
                        }
                }
                else
                {
                    Debug.Log("is local");

                    NetAction na = JsonUtility.FromJson<NetAction>(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    na.CurrentAction = Actions.Verification;
                    bytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(na));

                    IAsyncResult iar = localClient.BeginSend(bytes, bytes.Length, _clientsEP[i], null, null);
                    //IAsyncResult iar = local_listener.BeginSend(bytes, bytes.Length, ep2, null, null);
                    localClient.Client.EndSend(iar);

                    //Debug.Log("resending3");
                    //_sendQueue.Add(NA.Id, NA.MessageId);
                    if (!isQueueRechecker && NA.CurrentAction != Actions.Movement)
                        lock (_checkQueue)
                        {
                            NetChecker NCheck = new NetChecker();
                            NCheck.SendTime = Environment.TickCount;
                            NCheck.NetId = NA.NetId;
                            NCheck.Id = NA.Id;
                            NCheck.EP = _clientsEP[i];
                            NCheck.BytesData = bytes;

                            NCheck.UPDConnection = localClient;
                            NCheck.NA = NA;
                            NCheck.Tries = 1;

                            _checkQueue.Add(NA.MessageId, NCheck);
                            //_checkQueue.Add(NA.MessageId, NCheck);
                            //_checkQueue.Add(NA.MessageId, NCheck);
                            //Debug.Log(_checkQueue.Count.ToString());
                        }
                }
            }
    }
    
    private void ReSendQueueHandler()
    {
        while (true)
        {
                    //Debug.Log("resending123");
            lock (_checkQueue)
                for (int i = 0; i < _checkQueue.Count; i++)
                {
                    //_checkQueue.Remove(_checkQueue.ElementAt(i).Key);
                    //Debug.Log("resending");
                    int time = Environment.TickCount - _checkQueue.ElementAt(i).Value.SendTime;

                    //Debug.Log("Try " + _checkQueue.ElementAt(i).Value.Tries);

                    Debug.Log("try  " + _checkQueue.ElementAt(i).Value.Tries);

                    if (_checkQueue.ElementAt(i).Value.Tries > 10)
                    {
                        _sendQueue.Remove(_checkQueue.ElementAt(i).Key);
                        return;
                    }
                    
                    if (_checkQueue.ElementAt(i).Value.NA.CurrentAction != Actions.Movement)
                    {
                        _checkQueue.ElementAt(i).Value.Tries++;
                        SendData(_checkQueue.ElementAt(i).Value.BytesData, _checkQueue.ElementAt(i).Value.NA, _checkQueue.ElementAt(i).Value.UPDConnection, true);
                    }

                    //Thread.Sleep(1);
                }

            if (_checkQueue.Count > 100)
                _checkQueue.Clear();

            //Thread.Sleep(1);
        }

        Thread.CurrentThread.Interrupt();
    }

    private void TimeoutService()
    {
        /*while (Thread.CurrentThread.IsAlive)
        {
            for (int i = 0; i < _clientPing.Count; i++)
            {
                int ping = Environment.TickCount - _clientPing[i];
                //Debug.Log(_clientsId[i] + " " + ping);
                if (ping >= 5000)
                {
                    Console.WriteLine(_clientsId[i] + " has been kicked for: Timeout");
                    Terminal.Log("{0}", _clientsId[i] + " has been kicked for: Timeout");
                    _clientsEP.RemoveAt(i);
                    _clientsIP.RemoveAt(i);
                    _clientsPort.RemoveAt(i);
                    //_clientsData.RemoveAt(i);
                    _clientsId.RemoveAt(i);
                    _clientPing.RemoveAt(i);
                    _threads[i].Interrupt();
                    _CurrentPlayers--;
                }

                Thread.Sleep(100);
            }
        }*/
        Thread.CurrentThread.Interrupt();
    }
}