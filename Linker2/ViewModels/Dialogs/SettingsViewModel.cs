using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Model;
using Linker2.Validators;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Linker2.ViewModels.Dialogs;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    [ObservableProperty]
    public partial bool ClearClipboardWhenSessionStops { get; set; }

    [ObservableProperty]
    public partial string OpenLinkCommands { get; set; }

    [ObservableProperty]
    public partial string OpenLinkArguments { get; set; }

    [ObservableProperty]
    public partial string LockAfterSeconds { get; set; }

    [ObservableProperty]
    public partial string? GeckoDriverPath { get; set; }

    [ObservableProperty]
    public partial string ThumbnailImageIds { get; set; }

    [ObservableProperty]
    public partial string DefaultTag { get; set; }

    [ObservableProperty]
    public partial bool QuitWhenSessionTimeouts { get; set; }

    [ObservableProperty]
    public partial bool DeselectFileWhenSessionTimeouts { get; set; }

    [ObservableProperty]
    public partial string LinkFilesDirectoryPath { get; set; }

    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly ISessionUpdater sessionUpdater;

    public SettingsViewModel(IFileSystem fileSystem, IDialogs dialogs, ISessionUpdater sessionUpdater, ISettingsProvider settingsProvider)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.sessionUpdater = sessionUpdater;

        var settings = settingsProvider.Settings;
        ShowDetails = settings.ShowDetails;
        ClearClipboardWhenSessionStops = settings.ClearClipboardWhenSessionStops;
        OpenLinkCommands = settings.OpenLinkCommand;
        OpenLinkArguments = settings.OpenLinkArguments;
        LockAfterSeconds = settings.LockAfterSeconds.ToString();
        DefaultTag = settings.DefaultTag;
        GeckoDriverPath = settings.GeckoDriverPath;
        ThumbnailImageIds = string.Join(',', settings.ThumbnailImageIds);
        QuitWhenSessionTimeouts = settings.QuitWhenSessionTimeouts;
        DeselectFileWhenSessionTimeouts = settings.DeselectFileWhenSessionTimeouts;
        LinkFilesDirectoryPath = settings.LinkFilesDirectoryPath ?? string.Empty;
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
            DeselectFileWhenSessionTimeouts,
            string.IsNullOrEmpty(LinkFilesDirectoryPath) ? null : LinkFilesDirectoryPath);

        var settingsValidator = new SettingsDtoValidator();
        var validationResult = settingsValidator.Validate(settings);
        if (!validationResult.IsValid)
        {
            await dialogs.ShowErrorDialogAsync(validationResult);
            return;
        }

        sessionUpdater.UpdateSettings(settings);
        Messenger.Send<CloseDialog>();
    }
}
