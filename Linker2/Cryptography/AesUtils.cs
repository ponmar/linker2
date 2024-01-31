using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Linker2.Cryptography;

public class AesUtils
{
    public const int DefaultKeySize = 128;
    public const int DefaultBlockSize = 128;
    public const PaddingMode DefaultPadding = PaddingMode.PKCS7;

    public static string SecureStringToString(SecureString secureString)
    {
        return new NetworkCredential("", secureString).Password;
    }

    public static SecureString StringToSecureString(string text)
    {
        return new NetworkCredential("", text).SecurePassword;
    }

    public static byte[] PasswordToKey(SecureString secureString)
    {
        string password = SecureStringToString(secureString);
        return PasswordToKey(password);
    }

    public static byte[] PasswordToKey(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        return SHA256.HashData(passwordBytes);
    }
}
