using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Model;
using Linker2.Validators;
using System.IO.Abstractions;
using System.Linq;

namespace Linker2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool showDetails;

    [ObservableProperty]
    private bool clearClipboardWhenSessionStops;

    [ObservableProperty]
    private string openLinkCommand;

    [ObservableProperty]
    private string openLinkArguments;

    [ObservableProperty]
    private string lockAfterSeconds;

    [ObservableProperty]
    private string? geckoDriverPath;

    [ObservableProperty]
    private string thumbnailImageIds;

    [ObservableProperty]
    private string defaultTag;

    [ObservableProperty]
    private bool quitWhenSessionTimeouts;

    [ObservableProperty]
    private bool deselectFileWhenSessionTimeouts;

    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly ISessionSaver sessionSaver;

    public SettingsViewModel(IFileSystem fileSystem, IDialogs dialogs, ISessionSaver sessionSaver, SettingsDto settings)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.sessionSaver = sessionSaver;

        showDetails = settings.ShowDetails;
        clearClipboardWhenSessionStops = settings.ClearClipboardWhenSessionStops;
        openLinkCommand = settings.OpenLinkCommand;
        openLinkArguments = settings.OpenLinkArguments;
        lockAfterSeconds = settings.LockAfterSeconds.ToString();
        defaultTag = settings.DefaultTag;
        geckoDriverPath = settings.GeckoDriverPath;
        thumbnailImageIds = string.Join(',', settings.ThumbnailImageIds);
        quitWhenSessionTimeouts = settings.QuitWhenSessionTimeouts;
        DeselectFileWhenSessionTimeouts = settings.DeselectFileWhenSessionTimeouts;
    }

    [RelayCommand]
    private void BrowseGeckoDriverDirectoryPath()
    {
        var title = "Browse Gecko driver installation directory";
        string? dir;
        if (string.IsNullOrEmpty(GeckoDriverPath))
        {
            dir = dialogs.ShowBrowseExistingDirectoryDialog(title);
        }
        else
        {
            dir = dialogs.ShowBrowseExistingDirectoryDialog(title, GeckoDriverPath);
        }

        if (dir is not null)
        {
            GeckoDriverPath = dir;
        }
    }

    [RelayCommand]
    private void DownloadGeckoDriver()
    {
        dialogs.OpenUrlInDefaultBrowser(@"https://sourceforge.net/projects/geckodriver.mirror/");
    }

    [RelayCommand]
    private void Save()
    {
        if (!int.TryParse(LockAfterSeconds, out var lockAfterSecondsInt))
        {
            dialogs.ShowErrorDialog($"Invalid lock after seconds value");
            return;
        }

        var thumbnailImageIdParts = ThumbnailImageIds.Split(',');
        var thumbnailImageIdList = thumbnailImageIdParts.Select(x => x.Trim()).ToList();

        var settings = new SettingsDto(
            OpenLinkCommand,
            OpenLinkArguments,
            DefaultTag,
            lockAfterSecondsInt,
            string.IsNullOrEmpty(GeckoDriverPath) ? null : GeckoDriverPath,
            thumbnailImageIdList,
            ShowDetails,
            ClearClipboardWhenSessionStops,
            QuitWhenSessionTimeouts,
            DeselectFileWhenSessionTimeouts);

        var settingsValidator = new SettingsDtoValidator(fileSystem);
        var validationResult = settingsValidator.Validate(settings);
        if (!validationResult.IsValid)
        {
            dialogs.ShowErrorDialog(validationResult);
            return;
        }

        sessionSaver.SaveSettings(settings);
        Events.Send(new CloseDialog());
    }
}
