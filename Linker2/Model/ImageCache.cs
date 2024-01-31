using Avalonia.Media.Imaging;
using Linker2.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;

namespace Linker2.Model;

public class ImageCache
{
    private readonly IFileSystem fileSystem;

    private readonly string directoryPath;

    private readonly Dictionary<string, Bitmap> images = [];

    private readonly AesByteArrayOperation crypto = new();

    private readonly byte[] aesKey;

    private static readonly HttpClient httpClient = new();

    public ImageCache(IFileSystem fileSystem, string directoryPath, byte[] aesKey)
    {
        this.fileSystem = fileSystem;
        this.aesKey = aesKey;
        this.directoryPath = directoryPath;

        if (!fileSystem.Directory.Exists(directoryPath))
        {
            fileSystem.Directory.CreateDirectory(directoryPath);
        }
    }

    private string FilenameHashToFilePath(string url)
    {
        var filename = Md5Utils.CreateChecksum(url);
        return Path.Combine(directoryPath, filename);
    }

    public void Remove(string filenameHash)
    {
        var filePath = FilenameHashToFilePath(filenameHash);
        if (fileSystem.File.Exists(filePath))
        {
            fileSystem.File.Delete(filePath);
        }
    }

    public Bitmap Add(string filenameHash, string imageUrl)
    {
        var filePath = FilenameHashToFilePath(filenameHash);

        if (images.TryGetValue(filePath, out Bitmap? value))
        {
            return value;
        }

        byte[]? bytes = null;
        if (fileSystem.File.Exists(filePath))
        {
            var encryptedBytes = fileSystem.File.ReadAllBytes(filePath);
            try
            {
                bytes = crypto.Decrypt(encryptedBytes, aesKey);
            }
            catch
            {
                // Unable to decrypt file. The file is overwritten when successfully downloaded
            }
        }

        if (bytes is null)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, imageUrl);
            var result = httpClient.SendAsync(msg).Result;
            bytes = result.Content.ReadAsByteArrayAsync().Result;
            var encryptedBytes = crypto.Encrypt(bytes, aesKey);
            fileSystem.File.WriteAllBytes(filePath, encryptedBytes);
        }

        var image = ToImage(bytes);
        images[filePath] = image;
        return image;
    }

    private static Bitmap ToImage(byte[] array)
    {
        using var ms = new MemoryStream(array);
        return new Bitmap(ms);
    }

    public void ChangeKey(byte[] newAesKey)
    {
        foreach (var item in images)
        {
            var filePath = item.Key;
            var encryptedBytes = fileSystem.File.ReadAllBytes(filePath);
            var bytes = crypto.Decrypt(encryptedBytes, aesKey);
            var newEncryptedBytes = crypto.Encrypt(bytes, newAesKey);
            fileSystem.File.WriteAllBytes(filePath, newEncryptedBytes);
        }
    }
}
