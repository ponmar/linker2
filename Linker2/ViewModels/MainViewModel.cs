﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Cryptography;
using Linker2.Model;
using Linker2.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkIsSelected))]
    private LinkDto? selectedLink = null;

    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly ISessionUtils sessionUtils;
    private readonly IFileUtils fileUtils;
    private readonly ILinkModification linkModification;
    private readonly ILinkRepository linkRepository;
    private readonly ISettingsRepository settingsRepo;

    public MainViewModel(IFileSystem fileSystem, IDialogs dialogs, ISessionUtils sessionUtils, IFileUtils fileUtils, ILinkModification linkModification, ILinkRepository linkRepository, ISettingsRepository settingsRepo)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.sessionUtils = sessionUtils;
        this.fileUtils = fileUtils;
        this.linkModification = linkModification;
        this.linkRepository = linkRepository;
        this.settingsRepo = settingsRepo;

        UpdateAvailabelConfigFiles();

        this.RegisterForEvent<StartEditLink>((m) => AddOrEditLink(m.Link));
        this.RegisterForEvent<StartAddLink>((m) => AddOrEditLink(null));
        this.RegisterForEvent<OpenLink>((m) => sessionUtils.OpenLinkWithExternalProgram(m.Url));
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
        Filenames.Clear();
        foreach (var configFile in fileUtils.GetAvailableConfigFiles())
        {
            Filenames.Add(configFile);
        }
        if (Filenames.Count > 0)
        {
            SelectedFilename = Filenames.First();
        }
    }

    [RelayCommand]
    private void OpenAddLink()
    {
        AddOrEditLink(null);
    }

    [RelayCommand]
    private void OpenEditLink()
    {
        AddOrEditLink(SelectedLink);
    }

    private static void AddOrEditLink(LinkDto? link)
    {
        var addLinkViewModel = ServiceLocator.Resolve<AddLinkViewModel>("linkToEdit", link);
        var addLinkWindow = new AddLinkWindow(addLinkViewModel);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            addLinkWindow.ShowDialog(desktop.MainWindow!);
        }
    }

    [RelayCommand]
    private void OpenLink()
    {
        sessionUtils.OpenLinkWithExternalProgram(SelectedLink!.Url);
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

        if (SelectedLink is not null && SelectedLink.Url == link.Url)
        {
            SelectedLink = null;
        }
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
        var createWindow = new CreateWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await createWindow.ShowDialog(desktop.MainWindow!);
            UpdateAvailabelConfigFiles();
        }
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
        fileUtils.LocateConfigFile(SelectedFilename!);
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
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(Session!.Data.Settings);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            settingsWindow.ShowDialog(desktop.MainWindow!);
        }
    }

    [RelayCommand]
    private void OpenChangePassword()
    {
        var passwordWindow = new PasswordWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            passwordWindow.ShowDialog(desktop.MainWindow!);
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        var jsonFileType = new FilePickerFileType("Json files")
        {
            Patterns = new[] { "*.json" },
        };
        var exportFilePath = await dialogs.SelectNewFileDialogAsync("Export", initialDirectory, jsonFileType);

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
                SelectFileInExplorer(exportFilePath);
            }
            catch (Exception e)
            {
                await dialogs.ShowErrorDialogAsync($"Unable to export links to {exportFilePath}:\n{e.Message}");
            }            
        }
    }

    public static void SelectFileInExplorer(string path)
    {
        var explorerPath = path.Replace("/", @"\");
        Process.Start("explorer.exe", "/select, " + explorerPath);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        var exportedFileType = new FilePickerFileType("Linker exported file (.json)")
        {
            Patterns = new[] { "*.json" },
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
}
