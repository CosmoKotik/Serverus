using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Client.Core
{
    internal class Packet
    {
        public byte[] bytes { get; set; } = default!;
        public IPEndPoint EP { get; set; } = default!;
        public int PacketUID { get; set; }
    }
}
