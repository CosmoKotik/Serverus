using Client.Core;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Config.LoadConfigs();

            Network.Connect();
        }
    }
}