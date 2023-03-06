using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer.Core
{
    internal class ServerTypes
    {
        public enum ServerType 
        { 
            Auth,
            Game,
            Voice
        }
    }
}
