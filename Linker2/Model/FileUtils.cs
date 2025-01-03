using Linker2.Configuration;
using Linker2.Validators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Security;

namespace Linker2.Model;

public interface IFileUtils
{
    public IEnumerable<string> GetAvailableConfigFiles();
    void Create(string filename, SecureString password);
    void BackupConfigFile(string filename);
    void LocateConfigFile(string filename);
    void SelectFileInExplorer(string path);
    void Export(string filePath, DataDto data);
}

public class FileUtils : IFileUtils
{
    public const string FileSearchPattern = "*.linker";
    private const string BackupFileTimestampFormat = "yyyy-MM-ddTHHmmss";

    private readonly IFileSystem fileSystem;

    private static readonly DataDto DefaultConfig = new(
        new(OpenLinkCommand: @"C:\Program Files\Mozilla Firefox\firefox.exe",
            OpenLinkArguments: "-private-window %URL%",
            LockAfterSeconds: 200,
            DefaultTag: "New",
            GeckoDriverPath: null,
            ThumbnailImageIds: [],
            ShowDetails: true,
            ClearClipboardWhenSessionStops: true,
            QuitWhenSessionTimeouts: false,
            DeselectFileWhenSessionTimeouts: false),
        Links: [],
        Filters: new(null, null, null, [], false, null, OrderBy.Time, false),
        SelectedUrl: null);

    public FileUtils(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public IEnumerable<string> GetAvailableConfigFiles()
    {
        return EncryptedApplicationConfig<DataDto>.GetAvailableConfigFiles(fileSystem, Constants.AppName, FileSearchPattern);
    }

    public void BackupConfigFile(string filename)
    {
        var filePath = GetConfigFilePath(filename);
        var destinationDir = Path.GetDirectoryName(filePath);
        var destinationFilename = Path.GetFileNameWithoutExtension(filePath) + "_" + DateTime.Now.ToString(BackupFileTimestampFormat) + ".backup";
        var destinationPath = Path.Combine(destinationDir!, destinationFilename);

        fileSystem.File.Copy(filePath, destinationPath);
    }

    private string GetConfigFilePath(string filename)
    {
        var appDataConfig = new EncryptedApplicationConfig<DataDto>(fileSystem, Constants.AppName, filename);
        return appDataConfig.FilePath;
    }

    public void LocateConfigFile(string filename)
    {
        var filePath = GetConfigFilePath(filename);
        Process.Start("explorer.exe", "/select, " + filePath);
    }

    public void SelectFileInExplorer(string path)
    {
        var explorerPath = path.Replace("/", @"\");
        Process.Start("explorer.exe", "/select, " + explorerPath);
    }

    public void Create(string filename, SecureString password)
    {
        var result = new PasswordValidator().Validate(password);
        if (!result.IsValid)
        {
            throw new ValidationException(result);
        }

        var appDataConfig = new EncryptedApplicationConfig<DataDto>(fileSystem, Constants.AppName, filename);
        
        if (appDataConfig.FileExists())
        {
            throw new Exception("File already exists");
        }

        appDataConfig.Write(DefaultConfig, password);
    }

    public void Export(string filePath, DataDto data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        fileSystem.File.WriteAllText(filePath, json);
    }
}
