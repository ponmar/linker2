using System;
using System.IO;
using System.Security.Cryptography;

namespace Linker2.Cryptography;

public class AesStringOperation
{
    public int KeySize { get; set; } = AesUtils.DefaultKeySize;
    public int BlockSize { get; set; } = AesUtils.DefaultBlockSize;
    public PaddingMode Padding { get; set; } = AesUtils.DefaultPadding;

    public string Encrypt(string plainText, byte[] key)
    {
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Padding = Padding;

            aes.Key = key;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream();
            memoryStream.Write(aes.IV);

            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            array = memoryStream.ToArray();
        }

        return Convert.ToBase64String(array);
    }

    public string Decrypt(string cipherText, byte[] key)
    {
        byte[] buffer = Convert.FromBase64String(cipherText);
        using var memoryStream = new MemoryStream(buffer);

        byte[] iv = new byte[16];
        memoryStream.Read(iv, 0, iv.Length);

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);
        return streamReader.ReadToEnd();
    }
}
