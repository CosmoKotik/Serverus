using ServerusClient.Core;
using System;

public class Program
{
    public static async Task Main()
    {
        Network n = new Network();
        await n.ConnectAsync();
    }
}