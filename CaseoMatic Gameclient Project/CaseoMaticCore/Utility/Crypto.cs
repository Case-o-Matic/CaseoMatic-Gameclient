using System;
using System.IO;
using System.Security.Cryptography;

namespace CaseoMaticCore
{
    // http://dotnet-snippets.de/snippet/encrypt-and-decrypt-strings/205
    public class Crypto
    {
        private static byte[] entropy = new byte[4] { 0x4d, 0x76, 0x61, 0x6e };



        public static string EncryptString(string clearText)
        {
            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            byte[] encryptedData = EncryptString(clearBytes);
            return Convert.ToBase64String(encryptedData);
        }

        /// <returns></returns>
        public static string DecryptString(byte[] cipherBytes)
        {
            byte[] decryptedData = ProtectedData.Unprotect(cipherBytes, entropy, DataProtectionScope.LocalMachine); ;
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }

        private static byte[] EncryptString(byte[] clearText)
        {
            return ProtectedData.Protect(clearText, entropy, DataProtectionScope.LocalMachine);
        }
    } 
}