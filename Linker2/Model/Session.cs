using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security;
using Avalonia.Threading;
using Linker2.Configuration;
using Linker2.Cryptography;
using Linker2.HttpHelpers;

namespace Linker2.Model;

public class Session
{
    private readonly IFileSystem fileSystem;

    public string FilePath { get; }
    private SecureString password;
    public DataDto Data { get; set; }

    public DateTime StartedAt = DateTime.Now;
    private readonly TimeSpan timeout;

    private bool TimedOut => DateTime.Now > StartedAt + timeout;
    public TimeSpan TimeLeft => StartedAt + timeout - DateTime.Now;

    public IWebPageScraper? webPageScraper = null;

    public ImageCache ImageCache { get; }

    public bool DataUpdated
    {
        get => dataUpdated;
        set
        {
            dataUpdated = value;
            Events.Send<DataUpdatedChanged>();
        }
    }
    private bool dataUpdated;

    private readonly DispatcherTimer sessionTimer = new();

    public Session(IFileSystem fileSystem, string filePath, SecureString password, DataDto configuration)
    {
        this.fileSystem = fileSystem;
        FilePath = filePath;
        this.password = password;
        Data = configuration;

        var cacheDir = Path.Combine(Path.GetDirectoryName(filePath)!, "Cache", Path.GetFileNameWithoutExtension(filePath));
        ImageCache = new(fileSystem, cacheDir, AesUtils.PasswordToKey(password));
        timeout = TimeSpan.FromSeconds(configuration.Settings.LockAfterSeconds);

        sessionTimer.Interval = TimeSpan.FromSeconds(1);
        sessionTimer.Tick += SessionTimer_Tick;
        sessionTimer.Start();

        Events.Send(new SessionStarted(this));
        SessionTimer_Tick(null, EventArgs.Empty);
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
            Events.Send(new SessionTick(this));
        }
    }

    public void ResetTime()
    {
        StartedAt = DateTime.Now;
        SessionTimer_Tick(null, EventArgs.Empty);
    }

    private void InitWebPageScraper()
    {
        if (webPageScraper is null)
        {
            if (!string.IsNullOrEmpty(Data.Settings.GeckoDriverPath))
            {
                try
                {
                    webPageScraper = new FirefoxWebPageScraper(Data.Settings.GeckoDriverPath, true);
                }
                catch (Exception e)
                {
                    var dialogs = ServiceLocator.Resolve<IDialogs>();
                    dialogs.ShowErrorDialog($"Reverting to default web page scraper due to exception when creating FirefoxWebPageScraper: {e.Message}");
                }
            }
            
            if (webPageScraper is null)
            {
                webPageScraper = new HtmlAgilityPackWebPageScraper();
            }
        }
    }

    public void LoadDataFromUrl(string url, out string? title, out List<string> thumbnailUrl)
    {
        InitWebPageScraper();
        webPageScraper!.Load(url);
        title = webPageScraper!.PageTitle;
        thumbnailUrl = webPageScraper!.GetImageSrcs(Data.Settings.ThumbnailImageIds);
    }

    public void Stop()
    {
        if (!sessionTimer.IsEnabled)
        {
            // Session already stopped
            return;
        }

        sessionTimer.Stop();
        webPageScraper?.Close();

        Events.Send(new SessionStopped(Data.Settings));
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
}
