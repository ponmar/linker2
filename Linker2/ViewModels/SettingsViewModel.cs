using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Model;
using Linker2.Validators;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Linker2.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool showDetails;

    [ObservableProperty]
    private bool clearClipboardWhenSessionStops;

    [ObservableProperty]
    private string openLinkCommands;

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

    public SettingsViewModel(IFileSystem fileSystem, IDialogs dialogs, ISessionSaver sessionSaver, ISettingsProvider settingsProvider)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.sessionSaver = sessionSaver;

        var settings = settingsProvider.Settings;
        showDetails = settings.ShowDetails;
        clearClipboardWhenSessionStops = settings.ClearClipboardWhenSessionStops;
        openLinkCommands = settings.OpenLinkCommand;
        openLinkArguments = settings.OpenLinkArguments;
        lockAfterSeconds = settings.LockAfterSeconds.ToString();
        defaultTag = settings.DefaultTag;
        geckoDriverPath = settings.GeckoDriverPath;
        thumbnailImageIds = string.Join(',', settings.ThumbnailImageIds);
        quitWhenSessionTimeouts = settings.QuitWhenSessionTimeouts;
        DeselectFileWhenSessionTimeouts = settings.DeselectFileWhenSessionTimeouts;
    }

    [RelayCommand]
    private async Task BrowseGeckoDriverDirectoryPathAsync()
    {
        var title = "Browse Gecko driver installation directory";
        string? dir;
        if (string.IsNullOrEmpty(GeckoDriverPath))
        {
            dir = await dialogs.ShowBrowseExistingDirectoryDialogAsync(title);
        }
        else
        {
            dir = await dialogs.ShowBrowseExistingDirectoryDialogAsync(title, GeckoDriverPath);
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
    private async Task SaveAsync()
    {
        if (!int.TryParse(LockAfterSeconds, out var lockAfterSecondsInt))
        {
            await dialogs.ShowErrorDialogAsync($"Invalid lock after seconds value");
            return;
        }

        var thumbnailImageIdParts = ThumbnailImageIds.Split(',');
        var thumbnailImageIdList = thumbnailImageIdParts.Select(x => x.Trim()).ToList();

        var settings = new SettingsDto(
            OpenLinkCommands,
            OpenLinkArguments,
            DefaultTag,
            lockAfterSecondsInt,
            string.IsNullOrEmpty(GeckoDriverPath) ? null : GeckoDriverPath,
            thumbnailImageIdList,
            ShowDetails,
            ClearClipboardWhenSessionStops,
            QuitWhenSessionTimeouts,
            DeselectFileWhenSessionTimeouts);

        var settingsValidator = new SettingsDtoValidator();
        var validationResult = settingsValidator.Validate(settings);
        if (!validationResult.IsValid)
        {
            await dialogs.ShowErrorDialogAsync(validationResult);
            return;
        }

        sessionSaver.SaveSettings(settings);
        Messenger.Send<CloseDialog>();
    }
}
