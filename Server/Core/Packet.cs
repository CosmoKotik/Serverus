using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    internal class Packet
    {
        public byte[]? bytes { get; set; }
        public IPEndPoint? EP { get; set; }
        public int PacketUID { get; set; }
    }
}
