using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using FluentValidation.Results;
using Linker2.Configuration;
using Linker2.ViewModels;
using Linker2.Views.Dialogs;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Linker2;

public interface IDialogs
{
    Task ShowInfoDialogAsync(string message);
    Task ShowWarningDialogAsync(string message);
    Task ShowErrorDialogAsync(string message);
    Task ShowErrorDialogAsync(IEnumerable<string> messages);
    Task ShowErrorDialogAsync(ValidationResult validationResult);
    Task<bool> ShowConfirmDialogAsync(string question);
    Task<string?> SelectNewFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType);
    Task<string?> BrowseExistingFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType);
    Task<string?> ShowBrowseExistingDirectoryDialogAsync(string title);
    Task<string?> ShowBrowseExistingDirectoryDialogAsync(string title, string initialDirectory);
    Task ShowAddLinkAsync();
    Task ShowEditLinkAsync(LinkDto link);
    Task ShowSettingsAsync();
    Task ShowChangePasswordAsync();
    Task ShowLinkThumbnailAsync(LinkDto linkDto);
    Task ShowCreateAsync();
    void OpenUrlInDefaultBrowser(string url);
}

public class Dialogs : IDialogs
{
    public async Task ShowInfoDialogAsync(string message)
    {
        await ShowDialogAsync(message, ButtonEnum.Ok, Icon.Info);
    }

    public async Task ShowWarningDialogAsync(string message)
    {
        await ShowDialogAsync(message, ButtonEnum.Ok, Icon.Warning);
    }

    public async Task ShowErrorDialogAsync(string message)
    {
        await ShowDialogAsync(message, ButtonEnum.Ok, Icon.Error);
    }

    public async Task ShowErrorDialogAsync(IEnumerable<string> messages)
    {
        await ShowErrorDialogAsync(string.Join("\n", messages));
    }

    public async Task ShowErrorDialogAsync(ValidationResult validationResult)
    {
        await ShowErrorDialogAsync(validationResult.Errors.Select(x => x.ErrorMessage));
    }

    public async Task<bool> ShowConfirmDialogAsync(string question)
    {
        var result = await ShowDialogAsync(question, ButtonEnum.YesNo, Icon.Question);
        return result == ButtonResult.Yes;
    }

    private async Task<ButtonResult> ShowDialogAsync(string message, ButtonEnum button, Icon icon)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, button, icon, windowStartupLocation: WindowStartupLocation.CenterOwner);
        
        var parent = GetParentWindow();
        if (parent is not null)
        {
            return await box.ShowWindowDialogAsync(parent);
        }
        else
        {
            return await box.ShowWindowAsync();
        }
    }
        
    public static Window? GetParentWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp ? desktopApp.MainWindow : null;
    }

    public async Task<string?> SelectNewFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            throw new NotSupportedException();
        }

        var topLevel = TopLevel.GetTopLevel(desktopApp.MainWindow);
        var suggestedStartLocation = await topLevel!.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices = [fileType],
        });

        return file?.Path.AbsolutePath;
    }

    public async Task<string?> BrowseExistingFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            throw new NotSupportedException();
        }

        var topLevel = TopLevel.GetTopLevel(desktopApp.MainWindow);
        var suggestedStartLocation = await topLevel!.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            SuggestedStartLocation = suggestedStartLocation,
            AllowMultiple = false,
            FileTypeFilter = [fileType],
        });

        return files.Count > 0 ? files[0].Path.AbsolutePath : null;
    }

    public async Task<string?> ShowBrowseExistingDirectoryDialogAsync(string title)
    {
        return await ShowBrowseExistingDirectoryDialogAsync(title, string.Empty);
    }

    public async Task<string?> ShowBrowseExistingDirectoryDialogAsync(string title, string initialDirectory)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            throw new NotSupportedException();
        }

        var topLevel = TopLevel.GetTopLevel(desktopApp.MainWindow);
        var suggestedStartLocation = await topLevel!.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);

        var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            SuggestedStartLocation = suggestedStartLocation,
            AllowMultiple = false,
        });

        return files.Count > 0 ? files[0].Path.AbsolutePath : null;
    }

    public async Task ShowAddLinkAsync()
    {
        await AddOrEditLink();
    }

    public async Task ShowEditLinkAsync(LinkDto link)
    {
        await AddOrEditLink(link);
    }

    private static async Task AddOrEditLink(LinkDto? link = null)
    {
        var addLinkViewModel = ServiceLocator.Resolve<AddLinkViewModel>("linkToEdit", link);
        var addLinkWindow = new AddOrEditLinkWindow() { DataContext = addLinkViewModel };
        await ShowDesktopWindow(addLinkWindow);
    }

    public async Task ShowSettingsAsync()
    {
        await ShowDesktopWindow(new SettingsWindow());
    }

    public async Task ShowChangePasswordAsync()
    {
        await ShowDesktopWindow(new PasswordWindow());
    }

    public async Task ShowLinkThumbnailAsync(LinkDto linkDto)
    {
        var linkVm = ServiceLocator.Resolve<LinksViewModel>().Links.FirstOrDefault(x => x.LinkDto == linkDto);
        if (linkVm?.ThumbnailImage is not null)
        {
            var openLinkThumbnailWindow = new ImageWindow();
            openLinkThumbnailWindow.SetImage(linkVm.ThumbnailImage);
            await ShowDesktopWindow(openLinkThumbnailWindow);
        }
    }

    public async Task ShowCreateAsync()
    {
        await ShowDesktopWindow(new CreateWindow());
    }

    private static async Task ShowDesktopWindow(Window window)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await window.ShowDialog(desktop.MainWindow!);
        }
    }

    public void OpenUrlInDefaultBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
