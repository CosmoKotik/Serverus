using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer.Core
{
    internal class Client
    {
        public int ClientID { get; set; }
        public long ServerID { get; set; }
        public IPEndPoint ClientEndPoint { get; set; } = default!;
    }
}
