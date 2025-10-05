using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Cryptography;
using Linker2.Model;
using Linker2.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Linker2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionStarted))]
    [NotifyPropertyChangedFor(nameof(SessionClosedAndFileSelected))]
    [NotifyPropertyChangedFor(nameof(UnsavedChanges))]
    private Session? session;

    public bool SessionStarted => Session is not null;

    public bool SessionClosedAndFileSelected => Session is null && SelectedFilename is not null;

    public bool UnsavedChanges => Session is not null && Session.DataUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionClosedAndFileSelected))]
    private string? selectedFilename = null;

    public ObservableCollection<string> Filenames { get; } = [];

    [ObservableProperty]
    private string title = Constants.AppName;

    [ObservableProperty]
    private string backgroundText = Constants.AppName;

    public bool LinkIsSelected => SelectedLink is not null;
    public bool LinkFileExists => SelectedLink is not null && linkFileRepo.LinkFileExists(SelectedLink);
    public bool LinkHasThumbnail => SelectedLink is not null && !string.IsNullOrEmpty(SelectedLink.ThumbnailUrl);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkIsSelected))]
    [NotifyPropertyChangedFor(nameof(LinkFileExists))]
    [NotifyPropertyChangedFor(nameof(LinkHasThumbnail))]
    private LinkDto? selectedLink = null;

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
        sessionUtils.OpenLinkWithExternalProgramAsync(linkDto);
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
    private void LocateLinkFilesDirectory()
    {
        if (Session?.Data.Settings.LinkFilesDirectoryPath is not null)
        {
            try
            {
                fileUtils.SelectFileInExplorer(Session.Data.Settings.LinkFilesDirectoryPath);
            }
            catch
            {
                // Windows only - ignore in Linux
            }
        }
    }

    [RelayCommand]
    private void UpdateLinkFiles()
    {
        Session?.UpdateAvailableLinkFiles();
    }
}
