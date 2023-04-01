using Client.Core;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Network.Connect("10.0.1.3", 50663);
        }
    }
}