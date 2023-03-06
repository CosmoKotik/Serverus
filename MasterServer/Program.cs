using MasterServer.Core;

public class Program
{
    public static void Main()
    { 
        Network n = new Network();
        n.StartServer();
    }
}