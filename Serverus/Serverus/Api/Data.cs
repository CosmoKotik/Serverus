using System;
using System.Collections.Generic;
using System.Text;

namespace Serverus.Api
{
    public class Data
    {
        public Player player { get; set; }
        public Position pos { get; set; }
        public Item item { get; set; }
        public Actions action { get; set; }
        public bool IsDisconnected { get; set; }
        public bool IsCheating { get; set; }
    }
}
