using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security;
using Linker2.Cryptography;
using Newtonsoft.Json;

namespace Linker2.Configuration;

public class EncryptedApplicationConfig<T>
{
    private readonly IFileSystem fileSystem;

    public string AppName { get; }
    public string Filename { get; }
    public string FilePath { get; }

    public static IEnumerable<string> GetAvailableConfigFiles(IFileSystem fileSystem, string appName, string searchPattern)
    {
        var directory = GetDirectory(appName);
        if (!fileSystem.Directory.Exists(directory))
        {
            fileSystem.Directory.CreateDirectory(directory);
        }
        var configFilePaths = fileSystem.Directory.GetFiles(directory, searchPattern).ToList();
        return configFilePaths.Select(x => Path.GetFileName(x));
    }

    public EncryptedApplicationConfig(IFileSystem fileSystem, string appName, string filename)
    {
        this.fileSystem = fileSystem;
        AppName = appName;
        Filename = filename;

        var directory = GetDirectory();
        FilePath = Path.Combine(directory, Filename);
    }

    public string GetDirectory()
    {
        return GetDirectory(AppName);
    }

    public static string GetDirectory(string appName)
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDirectory, appName);
    }

    public void Write(T config, SecureString securePassword)
    {
        var jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
        var key = AesUtils.PasswordToKey(securePassword);
        var encryptedJson = new AesStringOperation().Encrypt(jsonString, key);
        var directory = Path.GetDirectoryName(FilePath);
        fileSystem.Directory.CreateDirectory(directory!);
        fileSystem.File.WriteAllText(FilePath, encryptedJson);
    }

    public T Read(SecureString securePassword)
    {
        var encryptedJson = fileSystem.File.ReadAllText(FilePath);

        string json;
        try
        {
            var key = AesUtils.PasswordToKey(securePassword);
            json = new AesStringOperation().Decrypt(encryptedJson, key);
        }
        catch (Exception e)
        {
            throw new Exception("Invalid password", e);
        }
        
        return JsonConvert.DeserializeObject<T>(json)!;
    }

    public bool FileExists()
    {
        return fileSystem.File.Exists(FilePath);
    }
}
