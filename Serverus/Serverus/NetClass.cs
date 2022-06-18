using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using static NetAction;

[Serializable]
public class NetClass
{
    public bool IsPlayer;
    public bool IsBlock;
    public bool IsDestroyed;
    public bool IsStatic;

    public string Name;

    public int Id;
    public int NetId;
    public int BlockId;

    public Actions CurrentAction;

    public Vector3 Position;
    public Vector3 Rotation;
}


[Serializable]
public class NetAction
{
    public enum Actions { Spawn, Movement, Interaction, Placing, Animation, Verification }

    public Actions CurrentAction;
    public int MessageId;
    public int NetId;
    public int Id;
    public int SendTime;
    public string ActionObject;

    public bool IsCheckingDataLoss;
    //public List<object> CurrentActions;
}

[Serializable]
public class NetMovement
{
    public int Id;

    public Vector3 Position;
    public Vector3 Rotation;
}

[Serializable]
public class NetInteraction
{
    public int Id;
    public int InteractedObjectId;

    public bool HasUsed; // true = use, false = attack
}

[Serializable]
public class NetPlacing
{
    public int Id;
    public int Block;

    public bool IsDestroyed;

    public Vector3 Position;
    public Vector3 Rotation;
}

[Serializable]
public class NetChecker
{
    public int NetId;
    public int Id;

    public int Tries;
    public int SendTime;
    public byte[] BytesData;

    public EndPoint EP;
    public NetAction NA;
    public UdpClient UPDConnection;
}

[Serializable]
public class NetSpawning
{
    public int Id;

    public bool IsPlayer;
    public bool IsBlock;

    public Vector3 Position;
    public Vector3 Rotation;
}


[Serializable]
public class NetVerification
{
    public int NetId;

    public string Json;
}