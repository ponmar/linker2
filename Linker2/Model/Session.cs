using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
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
    private readonly ILinkFileRepository linkFileRepo;

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

    private readonly DispatcherTimer sessionTimer = new();

    public Session(IFileSystem fileSystem, ILinkFileRepository linkFileRepo, string filePath, SecureString password, DataDto data)
    {
        this.fileSystem = fileSystem;
        this.linkFileRepo = linkFileRepo;
        FilePath = filePath;
        this.password = password;
        this.data = data;

        var cacheDir = Path.Combine(Path.GetDirectoryName(filePath)!, "Cache", Path.GetFileNameWithoutExtension(filePath));
        ImageCache = new(fileSystem, cacheDir, AesUtils.PasswordToKey(password));
        timeout = TimeSpan.FromSeconds(data.Settings.LockAfterSeconds);

        UpdateAvailableLinkFiles();

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

        linkFileRepo.Clear();
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

    public void UpdateAvailableLinkFiles()
    {
        var linkFilePaths = new List<string>();
        var dirPath = Data.Settings.LinkFilesDirectoryPath;
        if (dirPath.HasContent() && fileSystem.Directory.Exists(dirPath))
        {
            foreach (var linkFilePath in fileSystem.Directory.GetFiles(dirPath!))
            {
                linkFilePaths.Add(linkFilePath);
            }
        }
        linkFileRepo.Update(linkFilePaths);
    }

    public void UpdateLink(LinkDto link)
    {
        var currentLink = Data.Links.FirstOrDefault(x => x.Url == link.Url);
        if (currentLink is null)
        {
            throw new ArgumentException("No such link to update");
        }

        var currentLinkIndex = Data.Links.IndexOf(currentLink);

        Data.Links[currentLinkIndex] = link;
        DataUpdated = true;

        Messenger.Send(new LinkUpdated(this, link));
    }

    public void AddLink(LinkDto link)
    {
        if (Data.Links.Any(x => x.Url == link.Url))
        {
            throw new ArgumentException("Link already exists");
        }

        Data.Links.Add(link);
        DataUpdated = true;

        Messenger.Send(new LinkAdded(this, link));
    }

    public void RemoveLink(string url)
    {
        var linkToRemove = Data.Links.First(x => x.Url == url);
        Data.Links.Remove(linkToRemove);
        DataUpdated = true;

        Messenger.Send(new LinkRemoved(this, linkToRemove));

        if (linkToRemove.ThumbnailUrl is not null)
        {
            ImageCache.Remove(linkToRemove.ThumbnailUrl);
        }
    }
}
