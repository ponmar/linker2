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
using System.Threading.Tasks;

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

public interface ISessionSaver
{
    void SaveSettings(SettingsDto settings);
    void SaveSelection(string? selectedUrl);
    void SaveFilters(FiltersDto filters);
}

public record ImportSettings(string FilePath, bool IncludeLinks, bool IncludeFilters, bool IncludeSettings);

public interface ISessionUtils
{
    void StartSession(string filename, SecureString password);
    void StopSession();
    bool SaveSession();
    void ResetSessionTime();
    Task OpenLinkWithExternalProgramAsync(LinkDto link);
    void Import(ImportSettings importSettings);
    void ChangePassword(SecureString currentPassword, SecureString newPassword);
}

public interface IWebPageScraperProvider
{
    public IWebPageScraper? Firefox { get; set; }

    public IWebPageScraper HtmlAgilityPack { get; }
}

public class Model : ILinkRepository, ILinkModification, ISessionSaver, ISessionUtils, IWebPageScraperProvider, ISettingsProvider
{
    private Session? session = null;

    public IEnumerable<LinkDto> Links => session is null ? Enumerable.Empty<LinkDto>() : session.Data.Links;

    public SettingsDto Settings => session!.Data.Settings;

    private readonly IFileSystem fileSystem;
    private readonly IClipboardService clipboardService;
    private readonly IDialogs dialogs;
    private readonly ILinkFileRepository linkFileRepo;

    public Model(IFileSystem fileSystem, IClipboardService clipboardService, IDialogs dialogs, ILinkFileRepository linkFileRepo)
    {
        this.fileSystem = fileSystem;
        this.clipboardService = clipboardService;
        this.dialogs = dialogs;
        this.linkFileRepo = linkFileRepo;
        this.RegisterForEvent<SessionStopped>(x => CleanupSession());
    }

    public bool SaveSession()
    {
        return session!.Save();
    }

    public void SaveSettings(SettingsDto settings)
    {
        var data = new DataDto(settings, session!.Data.Links, session!.Data.Filters, session!.Data.SelectedUrl);
        SaveData(data);
        Messenger.Send<SettingsUpdated>();
    }

    public void SaveSelection(string? selectedUrl)
    {
        var data = new DataDto(session!.Data.Settings, session!.Data.Links, session!.Data.Filters, selectedUrl);
        SaveData(data);
    }

    public void SaveFilters(FiltersDto filters)
    {
        var data = new DataDto(session!.Data.Settings, session!.Data.Links, filters, session!.Data.SelectedUrl);
        SaveData(data);
    }

    private void SaveData(DataDto data)
    {
        var validator = new DataDtoValidator();
        var result = validator.Validate(data);
        if (!result.IsValid)
        {
            throw new Exception("This should not happen");
        }

        session!.Data = data;
        session.DataUpdated = true;
    }

    public void AddLink(LinkDto link)
    {
        if (session is null)
        {
            throw new InvalidOperationException("Can not update link when session not running");
        }

        if (session.Data.Links.Any(x => x.Url == link.Url))
        {
            throw new ArgumentException("Link already exists");
        }

        session.Data.Links.Add(link);
        session.DataUpdated = true;

        Messenger.Send(new LinkAdded(session, link));
    }

    public void UpdateLink(LinkDto link)
    {
        if (session is null)
        {
            throw new InvalidOperationException("Can not update link when session not running");
        }

        var currentLink = session.Data.Links.FirstOrDefault(x => x.Url == link.Url);
        if (currentLink is null)
        {
            throw new ArgumentException("No such link to update");
        }

        var currentLinkIndex = session.Data.Links.IndexOf(currentLink);

        session.Data.Links[currentLinkIndex] = link;
        session.DataUpdated = true;

        Messenger.Send(new LinkUpdated(session, link));
    }

    public void RemoveLink(string url)
    {
        if (session is null)
        {
            throw new InvalidOperationException("Can not update link when session not running");
        }

        var linkToRemove = session.Data.Links.First(x => x.Url == url);
        session.Data.Links.Remove(linkToRemove);
        session.DataUpdated = true;

        Messenger.Send(new LinkRemoved(session, linkToRemove));

        if (linkToRemove.ThumbnailUrl is not null)
        {
            session.ImageCache.Remove(linkToRemove.ThumbnailUrl);
        }
    }

    public void ResetSessionTime()
    {
        session?.ResetTime();
    }

    public async Task OpenLinkWithExternalProgramAsync(LinkDto link)
    {
        if (session is not null)
        {
            var linkIndex = session.Data.Links.IndexOf(link);
            var openCounter = link.OpenCounter == long.MaxValue ? 0 : link.OpenCounter + 1;
            var updatedLink = new LinkDto(link.Title, link.Tags, link.Url, link.DateTime, link.Rating, link.ThumbnailUrl, openCounter);

            session.Data.Links[linkIndex] = updatedLink;
            session.DataUpdated = true;

            Messenger.Send(new LinkUpdated(session, updatedLink));

            var openLinkCommand = session.Data.Settings.OpenLinkCommand.Split(";").FirstOrDefault(x => fileSystem.File.Exists(x));
            if (openLinkCommand is null)
            {
                await dialogs.ShowErrorDialogAsync("No link command setting");
                return;
            }

            var linkFilePath = linkFileRepo.GetLinkFilePath(link);
            var openLinkArgs = linkFilePath is null ?
                session.Data.Settings.OpenLinkArguments.Replace(SettingsDtoValidator.UrlReplaceString, link.Url) :
                session.Data.Settings.OpenLinkArguments.Replace(SettingsDtoValidator.UrlReplaceString, '"' + linkFilePath + '"');

            ProcessRunner.Start(openLinkCommand, openLinkArgs);
        }
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

        if (session!.DataUpdated)
        {
            SaveSession();
        }

        if (session.Data.Settings.ClearClipboardWhenSessionStops)
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
        if (session is not null)
        {
            if (!session.HasPassword(newPassword))
            {
                throw new Exception("Wrong password");
            }

            var result = new PasswordValidator().Validate(newPassword);
            if (!result.IsValid)
            {
                throw new ValidationException(result);
            }

            session.ChangePassword(newPassword);
        }
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
            SaveSettings(dataToImport.Settings);
        }

        if (importSettings.IncludeFilters)
        {
            SaveFilters(dataToImport.Filters);
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
