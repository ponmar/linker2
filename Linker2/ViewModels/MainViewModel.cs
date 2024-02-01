using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
    [NotifyPropertyChangedFor(nameof(UnsavedChanges))]
    private Session? session;

    public bool SessionStarted => Session is not null;

    public bool UnsavedChanges => Session is not null && Session.DataUpdated;

    [ObservableProperty]
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
    private readonly IUrlDataFetcher urlDataFetcher;
    private readonly ISettingsRepository settingsRepo;

    public MainViewModel(IFileSystem fileSystem, IDialogs dialogs, ISessionUtils sessionUtils, IFileUtils fileUtils, ILinkModification linkModification, ILinkRepository linkRepository, IUrlDataFetcher urlDataFetcher, ISettingsRepository settingsRepo)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.sessionUtils = sessionUtils;
        this.fileUtils = fileUtils;
        this.linkModification = linkModification;
        this.linkRepository = linkRepository;
        this.urlDataFetcher = urlDataFetcher;
        this.settingsRepo = settingsRepo;

        UpdateAvailabelConfigFiles();

        this.RegisterForEvent<StartEditLink>((m) =>
        {
            AddOrEditLink(m.Link);
        });

        this.RegisterForEvent<StartAddLink>((m) =>
        {
            AddOrEditLink(null);
        });

        this.RegisterForEvent<OpenLink>((m) =>
        {
            sessionUtils.OpenLinkWithExternalProgram(m.Url);
        });

        this.RegisterForEvent<StartRemoveLink>(async (m) =>
        {
            await RemoveLinkAsync(m.Link);
        });

        this.RegisterForEvent<LinkSelected>((m) =>
        {
            SelectedLink = m.Link;
        });

        this.RegisterForEvent<LinkDeselected>((m) =>
        {
            SelectedLink = null;
        });

        this.RegisterForEvent<DataUpdatedChanged>((m) =>
        {
            OnPropertyChanged(nameof(UnsavedChanges));
        });

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

    private void AddOrEditLink(LinkDto? link)
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
            dialogs.ShowErrorDialog("No link selected");
            return;
        }

        await RemoveLinkAsync(SelectedLink);
    }

    private async Task RemoveLinkAsync(LinkDto link)
    {
        var removeConfirmed = await dialogs.ShowConfirmDialog($"Remove link '{link.Title ?? link.Url}'?");
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
    private void Open()
    {
        if (string.IsNullOrEmpty(SelectedFilename))
        {
            dialogs.ShowErrorDialog("No filename specified");
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            dialogs.ShowErrorDialog("No password specified.");
            return;
        }

        try
        {
            var securePassword = AesUtils.StringToSecureString(Password);
            sessionUtils.StartSession(SelectedFilename, securePassword);
        }
        catch (Validators.ValidationException e)
        {
            dialogs.ShowErrorDialog(e.Result);
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialog(e.Message);
        }
    }

    [RelayCommand]
    private void Create()
    {
        var createWindow = new CreateWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            createWindow.ShowDialog(desktop.MainWindow!);
        }

        UpdateAvailabelConfigFiles();
    }

    [RelayCommand]
    private void Backup()
    {
        if (string.IsNullOrEmpty(SelectedFilename))
        {
            dialogs.ShowErrorDialog("No filename specified");
            return;
        }

        try
        {
            fileUtils.BackupConfigFile(SelectedFilename);
            dialogs.ShowInfoDialogAsync("Backup file created");
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialog($"Unable to backup file:\n{e.Message}");
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
        // TODO
        /*
        var settingsWindow = new PasswordWindow()
        {
            Owner = Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();
        */
    }

    [RelayCommand]
    private void Export()
    {
        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        var exportFilePath = dialogs.SelectNewFileDialog("Export", initialDirectory, ".json", "Linker export (.json)|*.json");

        if (exportFilePath is not null)
        {
            if (fileSystem.File.Exists(exportFilePath))
            {
                dialogs.ShowErrorDialog($"File already exists: {exportFilePath}");
                return;
            }

            try
            {
                fileUtils.Export(exportFilePath, Session!.Data);
                SelectFileInExplorer(exportFilePath);
            }
            catch (Exception e)
            {
                dialogs.ShowErrorDialog($"Unable to export links to {exportFilePath}:\n{e.Message}");
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
        var filePath = dialogs.BrowseExistingFileDialog("Select file to import", initialDirectory, "Linker export (.json)|*.json");
        if (filePath is null)
        {
            return;
        }

        var includeLinks = await dialogs.ShowConfirmDialog($"Import links from {filePath}?");
        var includeFilters = await dialogs.ShowConfirmDialog($"Import filters from {filePath}?");
        var includeSettings = await dialogs.ShowConfirmDialog($"Import settings from {filePath}?");
        var importSettings = new ImportSettings(filePath, includeLinks, includeFilters, includeSettings);

        try
        {
            sessionUtils.Import(importSettings);
            dialogs.ShowInfoDialogAsync("Import finished successfully!");
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialog($"Import error: {e.Message}");
        }
    }
}
