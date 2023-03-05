using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerusClient.Core
{
    internal class Packet
    {
        public int PacketID { get; set; }
        public byte[] bytes { get; set; }
        public IPEndPoint ep { get; set; }
    }
}
