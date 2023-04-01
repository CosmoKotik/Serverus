using MasterServer.Core;

public class Program
{
    public static void Main()
    { 
        Network n = new Network();
        UdpHandler udp = new UdpHandler(n);
        n.StartServer();
        udp.StartServer();
    }
}