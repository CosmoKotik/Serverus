using System;

namespace Git_Git_Server
{
    class Program
    {
        private string _localIP = "";
        static void Main(string[] args)
        {
            LoadConfigs ld = new LoadConfigs();
            ld.Load_Local();
            Console.WriteLine("IPAddress: " + ld.GetLocalConfig("LOCAL_IP_ADDRESS"));
            Console.WriteLine("Password: " + ld.GetLocalConfig("SERVER_PASSWORD"));
            Console.WriteLine("Port: " + ld.GetLocalConfig("SERVER_PORT"));

            Network nk = new Network();
            nk.StartServer("192.168.0.32");
        }
    }
}
