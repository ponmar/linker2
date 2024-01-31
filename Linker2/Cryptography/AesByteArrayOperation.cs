using System;
using System.IO;
using System.Security.Cryptography;

namespace Linker2.Cryptography;

public class AesByteArrayOperation
{
    public int KeySize { get; set; } = AesUtils.DefaultKeySize;
    public int BlockSize { get; set; } = AesUtils.DefaultBlockSize;
    public PaddingMode Padding { get; set; } = AesUtils.DefaultPadding;

    public byte[] Encrypt(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Padding = Padding;

        aes.Key = key;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var encryptedData = PerformCryptography(data, encryptor);
        
        var result = new byte[aes.IV.Length + encryptedData.Length];
        aes.IV.CopyTo(result, 0);
        Array.Copy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);
        return result;
    }

    public byte[] Decrypt(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Padding = Padding;

        aes.Key = key;

        var iv = new byte[16];
        Array.Copy(data, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var encryptedData = new byte[data.Length - iv.Length];
        Array.Copy(data, iv.Length, encryptedData, 0, encryptedData.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        return PerformCryptography(encryptedData, decryptor);
    }

    private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
    {
        using var ms = new MemoryStream();
        using var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);
        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();

        return ms.ToArray();
    }
}
