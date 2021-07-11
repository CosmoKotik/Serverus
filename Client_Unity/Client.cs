using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static NetworkManager;

public class Client : MonoBehaviour
{
    public Transform Player;
    public Transform PlayerPrefab;

    public Minimap Map;

    public int PlayerID = 0;
    public string PlayerName;

    public ActionData ReceivedData { get; set; }
    public ActionData LocalData { get; set; }
    public bool ReceivedLocalData { get; set; } = false;

    //Network client
    private UdpClient client = new UdpClient();
    private Transform _players;
    private Thread _connectThread;

    //Local player data
    private Vector3 _oldPosition;
    private Vector3 _playerPosition;



    void Start()
    {
        _players = transform.GetChild(0);

        ReceivedData = new ActionData();
        LocalData = new ActionData();
    }

    private void OnApplicationQuit()
    {
        if (_connectThread != null || _connectThread.IsAlive)
            _connectThread.Abort();
    }

    private void Update()
    {
        _playerPosition = Player.position;

        lock (ReceivedData)
            if (ReceivedData.Name != "" || ReceivedData.NetworkId != 0)
                InstantiateNewPlayer(ReceivedData);
    }

    private void InstantiateNewPlayer(ActionData acData)
    {
        string name = acData.Name;
        Vector3 pos = acData.Position;
        Vector3 rot = acData.Rotation;

        GameObject plr = GameObject.Find(name);

        print(acData.NetworkId);

        if (name != PlayerName)
        {
            if (!plr)
            {
                GameObject obj = Instantiate(PlayerPrefab, _players).gameObject;
                obj.name = name;

                if (Map != null)
                    Map.AddNewPlayer(name, true);
            }
            else
            {
                plr.transform.position = pos;
                plr.transform.eulerAngles = rot;

                if (Map != null)
                    Map.ChangePosition(pos, name);
            }
        }
    }

    public bool CreateConnect(string Ip, int Port)
    {
        _playerPosition = Player.position;

        _connectThread = new Thread(() => Connect(Ip, Port));
        _connectThread.Start();

        Thread.Sleep(100);

        return _connectThread.IsAlive;
    }

    public void Connect(string Ip, int Port)
    {
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(Ip), Port);

        try
        {
            client.Connect(serverEP);
        }
        catch
        {
            print("failed");
            Thread.CurrentThread.Abort();
        }

        Vector3 playerPosition = new Vector3((float)Math.Round(_playerPosition.x, 2),
                                             (float)Math.Round(_playerPosition.y, 2),
                                             (float)Math.Round(_playerPosition.z, 2));


        PlayerData pdata = new PlayerData();
        pdata.Id = 0;
        pdata.Name = PlayerName;

        string jsonData = JsonUtility.ToJson(pdata);
        byte[] connectedMessage = Encoding.ASCII.GetBytes(jsonData);
        client.Send(connectedMessage, connectedMessage.Length);

        byte[] serverData = client.Receive(ref serverEP);
        PlayerData serverPlayerData = new PlayerData();
        serverPlayerData = JsonUtility.FromJson<PlayerData>(Encoding.ASCII.GetString(serverData));

        LocalData.NetworkId = serverPlayerData.Id;

        while (true)
        {
            string json = "";
            byte[] nothing = Encoding.ASCII.GetBytes(" ");

            if (ReceivedLocalData)
            {
                switch (LocalData.Type)
                {
                    case ActionType.Position:
                        PositionData posData = new PositionData();

                        json = JsonUtility.ToJson(posData);
                        break;
                    case ActionType.Rotation:
                        RotationData rotData = new RotationData();

                        json = JsonUtility.ToJson(rotData);
                        break;
                }

                byte[] msg = Encoding.ASCII.GetBytes(json);
                client.Send(msg, msg.Length);
            }
            else
                client.Send(nothing, nothing.Length);

            
            /*byte[] msg = Encoding.ASCII.GetBytes(json);

            if (playerPosition != _oldPosition)
                client.Send(msg, msg.Length);
            else
                client.Send(nothing, nothing.Length);*/

            //_oldPosition = playerPosition;

            if (client.Available > 0)
            {
                byte[] receivedData = client.Receive(ref serverEP);

                print(Encoding.ASCII.GetString(receivedData));

                if (Encoding.ASCII.GetString(receivedData) != " ")
                {
                    try
                    {
                        ActionData receivedPlayerData = JsonUtility.FromJson<ActionData>(Encoding.ASCII.GetString(receivedData));

                        lock (ReceivedData)
                        {
                            ReceivedData.Name = receivedPlayerData.Name;
                            ReceivedData.Position = receivedPlayerData.Position;
                            ReceivedData.Rotation = receivedPlayerData.Rotation;
                        }
                    }
                    catch { }
                }

                Thread.Sleep(10);
            }

            client.Close();
            Thread.CurrentThread.Abort();
        }
    }

    [Serializable]
    public class PlayerData
    {
        public string Name;
        public int Id;
    }
}