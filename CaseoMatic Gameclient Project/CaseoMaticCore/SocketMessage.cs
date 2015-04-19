using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaseoMaticCore
{
    public struct SocketMessage
    {
        public string type;
        public string[] data;

        public SocketMessage(string type, params string[] data)
        {
            this.type = type;
            this.data = data;
        }
    }
}
