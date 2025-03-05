using System;
using System.Security.Cryptography;
using System.Text;

namespace JCBSystem.common
{
    public class DataProtectorHelper
    {
        public static string Protect(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static string Unprotect(string encryptedData)
        {
            byte[] dataBytes = Convert.FromBase64String(encryptedData);
            byte[] decryptedData = ProtectedData.Unprotect(dataBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}
