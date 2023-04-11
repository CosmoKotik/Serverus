using MasterServer.Core;

public class Program
{
    public static Thread t1 = default!;
    public static Thread t2 = default!;
    public static void Main()
    {
        Config.LoadConfigs();

        Network n = new Network();
        UdpHandler udp = new UdpHandler(n);

        t1 = new Thread(() => n.StartServer());
        t2 = new Thread(() => udp.StartServer());

        t1.Start();
        t2.Start();
    }
}