using System;
using System.IO;
using System.Security.Cryptography;

namespace CaseoMaticCore
{
    // http://dotnet-snippets.de/snippet/encrypt-and-decrypt-strings/205
    public class Crypto
    {
        private static byte[] entropy = new byte[4] { 0x4d, 0x76, 0x61, 0x6e };

        private static byte[] EncryptString(byte[] clearText)
        {
            return ProtectedData.Protect(clearText, entropy, DataProtectionScope.LocalMachine);
        }

        public static string EncryptString(string clearText)
        {
            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            byte[] encryptedData = EncryptString(clearBytes);
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="cipherData">The cipher data.</param>
        /// <param name="Key">The key.</param>
        /// <param name="IV">The IV.</param>
        /// <returns></returns>
        private static byte[] DecryptString(byte[] cipherData)
        {
            return ProtectedData.Unprotect(cipherData, entropy, DataProtectionScope.LocalMachine);
        }

        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="cipherText">The cipher text.</param>
        /// <param name="Password">The password.</param>
        /// <returns></returns>
        public static string DecryptString(string cipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedData = DecryptString(cipherBytes);
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }
    } 
}