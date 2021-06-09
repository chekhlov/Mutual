using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mutual.Utilities
{
    class Crypto
    {
        static public string DecodeToBase64(string str)
        {
            return String.IsNullOrEmpty(str) ? null : Encoding.Default.GetString(Convert.FromBase64String(str));
        }

        static public string EncodeFromBase64(string str)
        {
            return String.IsNullOrEmpty(str) ? null : System.Convert.ToBase64String(Encoding.Default.GetBytes(str));
        }


        static public string StringToHashSHA256(string pswd)
        {
            SHA256 mySHA256 = SHA256Managed.Create();
            var hashValue = mySHA256.ComputeHash(Encoding.Default.GetBytes(pswd));
            return System.Convert.ToBase64String(hashValue);

        }

    }
}
