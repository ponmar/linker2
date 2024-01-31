using System;
using System.Text;
using System.Security.Cryptography;

namespace Linker2.Cryptography;

public class Md5Utils
{
    public static string CreateChecksum(string text)
    {
        return BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty);
    }
}
