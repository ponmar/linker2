using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security;
using Avalonia.Threading;
using Linker2.Configuration;
using Linker2.Cryptography;
using Linker2.Extensions;
using Linker2.HttpHelpers;

namespace Linker2.Model;

public class Session
{
    private readonly IFileSystem fileSystem;

    public string FilePath { get; }
    private SecureString password;
    public DataDto Data
    {
        get => data;
        set
        {
            data = value;
            timeout = TimeSpan.FromSeconds(data.Settings.LockAfterSeconds);
        }
    }
    private DataDto data;

    public DateTime StartedAt = DateTime.Now;
    private TimeSpan timeout;

    private bool TimedOut => DateTime.Now > StartedAt + timeout;
    public TimeSpan TimeLeft => StartedAt + timeout - DateTime.Now;

    public IWebPageScraper? Firefox { get; set; } = null;
    public IWebPageScraper HtmlAgilityPack { get; } = new HtmlAgilityPackWebPageScraper();

    public ImageCache ImageCache { get; }

    public bool DataUpdated
    {
        get => dataUpdated;
        set
        {
            dataUpdated = value;
            Messenger.Send<DataUpdatedChanged>();
        }
    }
    private bool dataUpdated;

    private readonly List<string> cachedFiles = [];

    private readonly DispatcherTimer sessionTimer = new();

    public Session(IFileSystem fileSystem, string filePath, SecureString password, DataDto data)
    {
        this.fileSystem = fileSystem;
        FilePath = filePath;
        this.password = password;
        this.data = data;

        var cacheDir = Path.Combine(Path.GetDirectoryName(filePath)!, "Cache", Path.GetFileNameWithoutExtension(filePath));
        ImageCache = new(fileSystem, cacheDir, AesUtils.PasswordToKey(password));
        timeout = TimeSpan.FromSeconds(data.Settings.LockAfterSeconds);

        UpdateAvailableCacheFiles();

        sessionTimer.Interval = TimeSpan.FromSeconds(1);
        sessionTimer.Tick += SessionTimer_Tick;
    }

    public void Start()
    {
        Messenger.Send(new SessionStarted(this));
        SessionTimer_Tick(null, EventArgs.Empty);
        sessionTimer.Start();
    }

    public bool HasPassword(SecureString password)
    {
        return this.password.ToString() == password.ToString();
    }

    private void SessionTimer_Tick(object? sender, EventArgs e)
    {
        if (TimedOut)
        {
            Stop();
        }
        else
        {
            Messenger.Send(new SessionTick(this));
        }
    }

    public void ResetTime()
    {
        StartedAt = DateTime.Now;
        SessionTimer_Tick(null, EventArgs.Empty);
    }

    public void Stop()
    {
        if (!sessionTimer.IsEnabled)
        {
            // Session already stopped
            return;
        }

        sessionTimer.Stop();
        
        Firefox?.Close();
        HtmlAgilityPack.Close();

        Messenger.Send(new SessionStopped(Data.Settings));
    }

    public void ChangePassword(SecureString newPassword)
    {
        password = newPassword;
        DataUpdated = true;
        Save();

        ImageCache.ChangeKey(AesUtils.PasswordToKey(newPassword));
    }

    public bool Save()
    {
        if (!DataUpdated)
        {
            return false;
        }

        var appDataConfig = new EncryptedApplicationConfig<DataDto>(fileSystem, Constants.AppName, FilePath);
        try
        {
            appDataConfig.Write(Data, password);
            DataUpdated = false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void UpdateAvailableCacheFiles()
    {
        cachedFiles.Clear();

        var dirPath = Data.Settings.CachedFileDirectoryPath;
        if (dirPath.HasContent() && fileSystem.Directory.Exists(dirPath))
        {
            foreach (var file in fileSystem.Directory.GetFiles(dirPath!))
            {
                cachedFiles.Add(file);
            }
        }
    }

    public string? GetCachedFileForLink(LinkDto link)
    {
        if (link.Title.HasContent())
        {
            var expectedFilenameWithoutExtension = string.Join("_", link.Title!.Split(Path.GetInvalidFileNameChars()));
            foreach (var cachedFile in cachedFiles)
            {
                if (expectedFilenameWithoutExtension == Path.GetFileNameWithoutExtension(cachedFile))
                {
                    return cachedFile;
                }
            }
        }

        return null;
    }
}
