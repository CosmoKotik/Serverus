using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerusServer.Core
{
    internal class Peer
    {
        public int Id { get; set; }
        public bool IsConnected { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Thread PeerThread { get; set; }

        public bool ReceivedData { get; set; }
        public byte[] Bytes { get; set; }
        public List<byte[]> ByteQueue { get; set; }
        public List<byte[]> ImportantByteQueue { get; set; }
    }
}
