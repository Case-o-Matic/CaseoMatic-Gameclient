using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace CaseoMaticCore
{
    public static class Serializer
    {
        private static readonly byte[] entropy = new byte[4] { 2, 0, 1, 0 };

        public static string SerializeAndEncryptObject<T>(T obj)
        {
            return Convert.ToBase64String(ProtectedData.Protect(Convert.FromBase64String(Json.JsonParser.Serialize<T>(obj)), entropy, DataProtectionScope.LocalMachine));
        }
        public static T DeserializeAndDecryptString<T>(string text)
        {
            return (T)Json.JsonParser.Deserialize<T>(Convert.ToBase64String(ProtectedData.Unprotect(ASCIIEncoding.ASCII.GetBytes(text), entropy, DataProtectionScope.LocalMachine)));
        }
    }
}
