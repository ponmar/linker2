using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Linker2.Configuration;
using Linker2.HttpHelpers;
using Linker2.Validators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Security;

namespace Linker2.Model;

public interface ILinkRepository
{
    IEnumerable<LinkDto> Links { get; }
}

public interface ISettingsProvider
{
    SettingsDto Settings { get; }
}

public interface ILinkModification
{
    void UpdateLink(LinkDto link);
    void AddLink(LinkDto link);
    void RemoveLink(string url);
}

public interface ISessionUpdater
{
    void UpdateSettings(SettingsDto settings);
    void UpdateSelection(string? selectedUrl);
    void UpdateFilters(FiltersDto filters);
}

public record ImportSettings(string FilePath, bool IncludeLinks, bool IncludeFilters, bool IncludeSettings);

public interface ISessionUtils
{
    void StartSession(string filename, SecureString password);
    void StopSession();
    bool SaveSession();
    void ResetSessionTime();
    void OpenLinkWithExternalProgramAsync(LinkDto link);
    void Import(ImportSettings importSettings);
    void ChangePassword(SecureString currentPassword, SecureString newPassword);
}

public interface IWebPageScraperProvider
{
    public IWebPageScraper? Firefox { get; set; }

    public IWebPageScraper HtmlAgilityPack { get; }
}

public class Model : ILinkRepository, ILinkModification, ISessionUpdater, ISessionUtils, IWebPageScraperProvider, ISettingsProvider
{
    private Session? session = null;

    public IEnumerable<LinkDto> Links => session is null ? Enumerable.Empty<LinkDto>() : session.Data.Links;

    public SettingsDto Settings => session!.Data.Settings;

    private readonly IFileSystem fileSystem;
    private readonly IClipboardService clipboardService;
    private readonly ILinkFileRepository linkFileRepo;

    public Model(IFileSystem fileSystem, IClipboardService clipboardService, ILinkFileRepository linkFileRepo)
    {
        this.fileSystem = fileSystem;
        this.clipboardService = clipboardService;
        this.linkFileRepo = linkFileRepo;
        this.RegisterForEvent<SessionStopped>(x => CleanupSession());
    }

    public bool SaveSession()
    {
        return session!.Save();
    }

    public void UpdateSettings(SettingsDto settings)
    {
        var data = new DataDto(settings, session!.Data.Links, session!.Data.Filters, session!.Data.SelectedUrl);
        session?.UpdateData(data);
        Messenger.Send<SettingsUpdated>();
    }

    public void UpdateSelection(string? selectedUrl)
    {
        var data = new DataDto(session!.Data.Settings, session!.Data.Links, session!.Data.Filters, selectedUrl);
        session?.UpdateData(data);
    }

    public void UpdateFilters(FiltersDto filters)
    {
        var data = new DataDto(session!.Data.Settings, session!.Data.Links, filters, session!.Data.SelectedUrl);
        session?.UpdateData(data);
    }

    public void AddLink(LinkDto link)
    {
        session?.AddLink(link);        
    }

    public void UpdateLink(LinkDto link)
    {
        session?.UpdateLink(link);
    }

    public void RemoveLink(string url)
    {
        session?.RemoveLink(url);
    }

    public void ResetSessionTime()
    {
        session?.ResetTime();
    }

    public void OpenLinkWithExternalProgramAsync(LinkDto link)
    {
        session?.OpenLinkWithExternalProgramAsync(link);
    }

    // Throws on errors
    public void StartSession(string filename, SecureString password)
    {
        var appDataConfig = new EncryptedApplicationConfig<DataDto>(fileSystem, Constants.AppName, filename);
        var config = appDataConfig.Read(password);

        var dataDtoValidator = new DataDtoValidator();
        var dataDtoValidatorResult = dataDtoValidator.Validate(config!);
        if (!dataDtoValidatorResult.IsValid)
        {
            throw new ValidationException(dataDtoValidatorResult);
        }

        session = new Session(fileSystem, linkFileRepo, appDataConfig.FilePath, password, config);
        session.Start();
    }

    public void StopSession()
    {
        session?.Stop();
    }

    private void CleanupSession()
    {
        Messenger.Send<SessionStopping>();

        SaveSession();

        if (session!.Data.Settings.ClearClipboardWhenSessionStops)
        {
            clipboardService.ClearAsync();
        }

        if (session.Data.Settings.QuitWhenSessionTimeouts)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
            {
                desktopApp.Shutdown();
            }
        }

        session = null;
    }

    public void ChangePassword(SecureString currentPassword, SecureString newPassword)
    {
        session?.ChangePassword(currentPassword, newPassword);
    }

    public void Import(ImportSettings importSettings)
    {
        if (session is null)
        {
            throw new Exception("Session closed");
        }

        var json = fileSystem.File.ReadAllText(importSettings.FilePath);
        var dataToImport = JsonConvert.DeserializeObject<DataDto>(json)!;

        if (importSettings.IncludeSettings)
        {
            UpdateSettings(dataToImport.Settings);
        }

        if (importSettings.IncludeFilters)
        {
            UpdateFilters(dataToImport.Filters);
        }

        if (importSettings.IncludeLinks)
        {
            foreach (var importedLink in dataToImport!.Links)
            {
                var currentLink = session.Data.Links.FirstOrDefault(x => x.Url == importedLink.Url);
                if (currentLink is not null)
                {
                    var mergedLink = LinkMerger.MergeLinks(currentLink, importedLink);
                    UpdateLink(mergedLink);
                }
                else
                {
                    AddLink(importedLink);
                }
            }
        }

        if (!SaveSession())
        {
            throw new Exception("Failed to save file after import");
        }
    }

    public IWebPageScraper? Firefox
    {
        get => session!.Firefox;
        set
        {
            session!.Firefox = value;
        }
    }

    public IWebPageScraper HtmlAgilityPack => session!.HtmlAgilityPack;
}
