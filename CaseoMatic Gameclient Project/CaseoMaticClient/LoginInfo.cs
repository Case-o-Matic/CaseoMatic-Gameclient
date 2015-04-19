using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaseoMaticClient
{
    public struct LoginInfo
    {
        public string username, email;
        public string dataId;

        public LoginInfo(string username, string email, string dataid)
        {
            this.username = username;
            this.email = email;
            this.dataId = dataid;
        }
    }
}
