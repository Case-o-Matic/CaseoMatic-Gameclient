using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace CaseoMaticCore
{
    public static class Serializer
    {
        private static readonly byte[] entropy = new byte[4] { 2, 0, 1, 0 };

        public static string SerializeAndEncryptObject<T>(T obj)
        {
            return ASCIIEncoding.ASCII.GetString(ProtectedData.Protect(Convert.FromBase64String(JsonConvert.SerializeObject(obj)), entropy, DataProtectionScope.LocalMachine));
        }
        public static T DeserializeAndDecryptString<T>(string text)
        {
            return (T)JsonConvert.DeserializeObject<T>(ASCIIEncoding.ASCII.GetString(ProtectedData.Unprotect(ASCIIEncoding.ASCII.GetBytes(text), entropy, DataProtectionScope.LocalMachine)));
        }
    }
}
