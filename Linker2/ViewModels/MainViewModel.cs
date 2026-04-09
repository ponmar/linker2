using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Cryptography;
using Linker2.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Linker2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionStarted))]
    [NotifyPropertyChangedFor(nameof(SessionClosedAndFileSelected))]
    [NotifyPropertyChangedFor(nameof(UnsavedChanges))]
    public partial Session? Session { get; set; }

    public bool SessionStarted => Session is not null;

    public bool SessionClosedAndFileSelected => Session is null && SelectedFilename is not null;

    public bool UnsavedChanges => Session is not null && Session.DataUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionClosedAndFileSelected))]
    public partial string? SelectedFilename { get; set; } = null;
    public ObservableCollection<string> Filenames { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = Constants.AppName;

    [ObservableProperty]
    public partial string BackgroundText { get; set; } = Constants.AppName;

    public bool LinkIsSelected => SelectedLink is not null;
    public bool LinkFileExists => SelectedLink is not null && linkFileRepo.LinkFileExists(SelectedLink);
    public bool LinkHasThumbnail => SelectedLink is not null && !string.IsNullOrEmpty(SelectedLink.ThumbnailUrl);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkIsSelected))]
    [NotifyPropertyChangedFor(nameof(LinkFileExists))]
    [NotifyPropertyChangedFor(nameof(LinkHasThumbnail))]
    public partial LinkDto? SelectedLink { get; set; } = null;

    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly ISessionUtils sessionUtils;
    private readonly IFileUtils fileUtils;
    private readonly ILinkModification linkModification;
    private readonly IClipboardService clipboardService;
    private readonly ILinkFileRepository linkFileRepo;

    public MainViewModel(IFileSystem fileSystem, IDialogs dialogs, ISessionUtils sessionUtils, IFileUtils fileUtils, ILinkModification linkModification, IClipboardService clipboardService, ILinkFileRepository linkFileRepo)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.sessionUtils = sessionUtils;
        this.fileUtils = fileUtils;
        this.linkModification = linkModification;
        this.clipboardService = clipboardService;
        this.linkFileRepo = linkFileRepo;

        UpdateAvailabelConfigFiles();

        this.RegisterForEvent<StartEditLink>(async (m) => await dialogs.ShowEditLinkAsync(m.Link));
        this.RegisterForEvent<StartAddLink>(async (m) => await dialogs.ShowAddLinkAsync());
        this.RegisterForEvent<OpenLink>((m) => OpenLink(m.Link));
        this.RegisterForEvent<OpenLinkThumbnail>(async (m) => await OpenLinkThumbnailAsync(m.Link));
        this.RegisterForEvent<LocateLinkFile>(async (m) => await LocateFileForLinkAsync(m.Link));
        this.RegisterForEvent<CopyLinkFilePath>((m) => CopyLinkFilePath(m.Link));
        this.RegisterForEvent<CopyLinkUrl>((m) => CopyLinkUrl(m.Link));
        this.RegisterForEvent<CopyLinkTitle>((m) => CopyLinkTitle(m.Link));
        this.RegisterForEvent<StartRemoveLink>(async (m) => await RemoveLinkAsync(m.Link));
        this.RegisterForEvent<LinkSelected>((m) => SelectedLink = m.Link);
        this.RegisterForEvent<LinkDeselected>((m) => SelectedLink = null);
        this.RegisterForEvent<DataUpdatedChanged>((m) => OnPropertyChanged(nameof(UnsavedChanges)));

        this.RegisterForEvent<SessionStarted>((m) =>
        {
            SelectedFilename = Path.GetFileName(m.Session.FilePath);
            Session = m.Session;
        });

        this.RegisterForEvent<SessionTick>((m) =>
        {
            var pendingChanges = m.Session.DataUpdated ? " *" : string.Empty;
            Title = $"{Constants.AppName} - {m.Session.FilePath} ({((int)m.Session.TimeLeft.TotalSeconds) + 1}){pendingChanges}";
        });

        this.RegisterForEvent<SessionStopped>((m) =>
        {
            Title = Constants.AppName;
            Session = null;
            SelectedLink = null;
            if (m.Settings.DeselectFileWhenSessionTimeouts)
            {
                SelectedFilename = null;
            }
        });
    }

    private void UpdateAvailabelConfigFiles()
    {
        var prevSelectedFilename = SelectedFilename;
        
        Filenames.Clear();
        foreach (var configFile in fileUtils.GetAvailableConfigFiles())
        {
            Filenames.Add(configFile);
        }

        if (prevSelectedFilename is not null && Filenames.Contains(prevSelectedFilename))
        {
            SelectedFilename = prevSelectedFilename;
        }
        else if (Filenames.Count > 0)
        {
            SelectedFilename = Filenames.First();
        }
    }

    [RelayCommand]
    private async Task OpenAddLinkAsync()
    {
        await dialogs.ShowAddLinkAsync();
    }

    [RelayCommand]
    private async Task OpenEditLink()
    {
        if (SelectedLink is not null)
        {
            await dialogs.ShowEditLinkAsync(SelectedLink);
        }
    }

    [RelayCommand]
    private void OpenSelectedLink()
    {
        if (SelectedLink is not null)
        {
            OpenLink(SelectedLink);
        }
    }

    private void OpenLink(LinkDto linkDto)
    {
        try
        {
            sessionUtils.OpenLinkWithExternalProgramAsync(linkDto);
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialogAsync(e.Message);
        }
    }

    [RelayCommand]
    private async Task OpenSelectedLinkThumbnailAsync()
    {
        if (SelectedLink is not null)
        {
            await OpenLinkThumbnailAsync(SelectedLink);
        }
    }

    private async Task OpenLinkThumbnailAsync(LinkDto linkDto)
    {
        if (Session is not null && !string.IsNullOrEmpty(linkDto.ThumbnailUrl))
        {
            await dialogs.ShowLinkThumbnailAsync(linkDto);
        }
    }

    [RelayCommand]
    private async Task LocateFileForSelectedLinkAsync()
    {
        if (SelectedLink is not null)
        {
            await LocateFileForLinkAsync(SelectedLink);
        }
    }

    private async Task LocateFileForLinkAsync(LinkDto linkDto)
    {
        if (Session is not null)
        {
            var filePath = linkFileRepo.GetLinkFilePath(linkDto);
            if (filePath is null)
            {
                await dialogs.ShowErrorDialogAsync("No cached file for this link.");
                return;
            }

            if (!fileSystem.File.Exists(filePath))
            {
                await dialogs.ShowErrorDialogAsync($"Cached file not found: {filePath}");
                return;
            }

            try
            {
                fileUtils.SelectFileInExplorer(filePath);
            }
            catch
            {
                // Windows only - ignore in Linux
            }
        }
    }

    [RelayCommand]
    private void CopySelectedLinkFilePath()
    {
        if (SelectedLink is not null)
        {
            CopyLinkFilePath(SelectedLink);
        }
    }

    private void CopyLinkFilePath(LinkDto linkDto)
    {
        if (Session is not null)
        {
            var filePath = linkFileRepo.GetLinkFilePath(linkDto);
            if (filePath is not null)
            {
                clipboardService.SetTextAsync(filePath);
            }
        }
    }

    [RelayCommand]
    private void CopySelectedLinkUrl()
    {
        if (SelectedLink is not null)
        {
            CopyLinkUrl(SelectedLink);
        }
    }

    private void CopyLinkUrl(LinkDto linkDto)
    {
        clipboardService.SetTextAsync(linkDto.Url);
    }

    [RelayCommand]
    private void CopySelectedLinkTitle()
    {
        if (SelectedLink is not null)
        {
            CopyLinkTitle(SelectedLink);
        }
    }

    private void CopyLinkTitle(LinkDto linkDto)
    {
        clipboardService.SetTextAsync(linkDto.Title ?? string.Empty);
    }

    [RelayCommand]
    private async Task RemoveSelectedLinkAsync()
    {
        if (SelectedLink is null)
        {
            await dialogs.ShowErrorDialogAsync("No link selected");
            return;
        }

        await RemoveLinkAsync(SelectedLink);
    }

    private async Task RemoveLinkAsync(LinkDto link)
    {
        var removeConfirmed = await dialogs.ShowConfirmDialogAsync($"Remove link '{link.Title ?? link.Url}'?");
        if (!removeConfirmed)
        {
            return;
        }

        linkModification.RemoveLink(link.Url);
    }

    [RelayCommand]
    private void Refresh()
    {
        UpdateAvailabelConfigFiles();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilename))
        {
            await dialogs.ShowErrorDialogAsync("No filename specified");
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            await dialogs.ShowErrorDialogAsync("No password specified.");
            return;
        }

        try
        {
            var securePassword = AesUtils.StringToSecureString(Password);
            sessionUtils.StartSession(SelectedFilename, securePassword);
            Password = string.Empty;
        }
        catch (Validators.ValidationException e)
        {
            await dialogs.ShowErrorDialogAsync(e.Result);
        }
        catch (Exception e)
        {
            await dialogs.ShowErrorDialogAsync(e.Message);
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        await dialogs.ShowCreateAsync();
        UpdateAvailabelConfigFiles();
    }

    [RelayCommand]
    private void Backup()
    {
        try
        {
            fileUtils.BackupConfigFile(SelectedFilename!);
            dialogs.ShowInfoDialogAsync("Backup file created");
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialogAsync($"Unable to backup file:\n{e.Message}");
        }
    }

    [RelayCommand]
    private void Locate()
    {
        try
        {
            fileUtils.LocateConfigFile(SelectedFilename!);
        }
        catch
        {
            // Windows only - ignore in Linux
        }
    }

    [RelayCommand]
    private static void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    [RelayCommand]
    private void Save()
    {
        sessionUtils.SaveSession();
    }

    [RelayCommand]
    private void Close()
    {
        sessionUtils.StopSession();
        SelectedLink = null;
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await dialogs.ShowSettingsAsync();
    }

    [RelayCommand]
    private async Task OpenChangePassword()
    {
        await dialogs.ShowChangePasswordAsync();
    }

    [RelayCommand]
    private async Task ExportFilteredLinks()
    {
        var filteredLinks = ServiceLocator.Resolve<LinksViewModel>().Links.Select(x => x.LinkDto);
        if (!filteredLinks.Any())
        {
            await dialogs.ShowErrorDialogAsync($"No links to export");
            return;
        }

        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        var txtFileType = new FilePickerFileType("Text files")
        {
            Patterns = ["*.txt"],
        };
        var exportFilePath = await dialogs.SelectNewFileDialogAsync("Export links", initialDirectory, txtFileType);

        if (exportFilePath is not null)
        {
            if (fileSystem.File.Exists(exportFilePath))
            {
                await dialogs.ShowErrorDialogAsync($"File already exists: {exportFilePath}");
                return;
            }

            try
            {
                fileUtils.ExportLinks(exportFilePath, filteredLinks);
                try
                {
                    fileUtils.SelectFileInExplorer(exportFilePath);
                }
                catch
                {
                    // Windows only - ignore in Linux
                }
            }
            catch (Exception e)
            {
                await dialogs.ShowErrorDialogAsync($"Unable to export links to {exportFilePath}:\n{e.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportFilteredLinksPlaylist()
    {
        if (Session is null)
        {
            return;
        }

        var playlistDir = Session.Data.Settings.LinkFilesDirectoryPath;
        if (playlistDir is null)
        {
            await dialogs.ShowErrorDialogAsync($"No configured link files directory");
            return;
        }

        var filteredLinkVms = ServiceLocator.Resolve<LinksViewModel>().Links;
        if (!filteredLinkVms.Any())
        {
            await dialogs.ShowErrorDialogAsync($"No links to export");
            return;
        }

        var linkVmsWithFile = filteredLinkVms.Where(x => x.FileExists);
        if (!linkVmsWithFile.Any())
        {
            await dialogs.ShowErrorDialogAsync($"No links with file available");
            return;
        }

        var playlistPath = Path.Combine(playlistDir, "export.m3u8");
        if (fileSystem.File.Exists(playlistPath))
        {
            var overwriteConfirmed = await dialogs.ShowConfirmDialogAsync($"File {playlistPath} already exists. Overwrite?");
            if (!overwriteConfirmed)
            {
                return;
            }
        }

        var m3uContentSs = new StringBuilder();
        m3uContentSs.AppendLine("#EXTM3U");
        foreach (var linkDto in linkVmsWithFile.Select(x => x.LinkDto))
        {
            var absoluteFilePath = linkFileRepo.GetLinkFilePath(linkDto);
            var filePath = Path.GetFileName(absoluteFilePath);
            m3uContentSs.AppendLine($"#EXTINF:-1 {linkDto.Title ?? filePath}");
            m3uContentSs.AppendLine(filePath);
            m3uContentSs.AppendLine();
        }

        var playlistContent = m3uContentSs.ToString();
        fileSystem.File.WriteAllText(playlistPath, playlistContent);

        try
        {
            fileUtils.SelectFileInExplorer(playlistPath);
        }
        catch
        {
            // Windows only - ignore in Linux
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        var jsonFileType = new FilePickerFileType("Json files")
        {
            Patterns = ["*.json"],
        };
        var exportFilePath = await dialogs.SelectNewFileDialogAsync("Export session", initialDirectory, jsonFileType);

        if (exportFilePath is not null)
        {
            if (fileSystem.File.Exists(exportFilePath))
            {
                await dialogs.ShowErrorDialogAsync($"File already exists: {exportFilePath}");
                return;
            }

            try
            {
                fileUtils.Export(exportFilePath, Session!.Data);
                try
                {
                    fileUtils.SelectFileInExplorer(exportFilePath);
                }
                catch
                {
                    // Windows only - ignore in Linux
                }
            }
            catch (Exception e)
            {
                await dialogs.ShowErrorDialogAsync($"Unable to export links to {exportFilePath}:\n{e.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        var exportedFileType = new FilePickerFileType("Linker exported file (.json)")
        {
            Patterns = ["*.json"],
        };
        var filePath = await dialogs.BrowseExistingFileDialogAsync("Select file to import", initialDirectory, exportedFileType);
        if (filePath is null)
        {
            return;
        }

        var includeLinks = await dialogs.ShowConfirmDialogAsync($"Import links from {filePath}?");
        var includeFilters = await dialogs.ShowConfirmDialogAsync($"Import filters from {filePath}?");
        var includeSettings = await dialogs.ShowConfirmDialogAsync($"Import settings from {filePath}?");
        var importSettings = new ImportSettings(filePath, includeLinks, includeFilters, includeSettings);

        try
        {
            sessionUtils.Import(importSettings);
            await dialogs.ShowInfoDialogAsync("Import finished successfully!");
        }
        catch (Exception e)
        {
            await dialogs.ShowErrorDialogAsync($"Import error: {e.Message}");
        }
    }

    [RelayCommand]
    private async Task LocateLinkFilesDirectoryAsync()
    {
        var linkFilesDirPath = Session?.Data.Settings.LinkFilesDirectoryPath;
        if (linkFilesDirPath is not null)
        {
            if (!fileSystem.Directory.Exists(linkFilesDirPath))
            {
                await dialogs.ShowErrorDialogAsync($"Link files directory does not exist: {linkFilesDirPath}");
                return;
            }

            try
            {
                fileUtils.SelectFileInExplorer(linkFilesDirPath);
            }
            catch
            {
                // Windows only - ignore in Linux
            }
        }
    }

    [RelayCommand]
    private void UpdateAvailableLinkFiles()
    {
        Session?.UpdateAvailableLinkFiles();
    }
}
