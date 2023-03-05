using ServerusServer.Core;
using System;

public class Program
{
    public static void Main()
    {
        Network n = new Network();
        n.StartServerAsync();
        Console.WriteLine("asdasd");
    }
}