using FluentValidation.Results;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using System;

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
    void OpenUrlInDefaultBrowser(string url);
}

public class Dialogs : IDialogs
{
    public async Task ShowInfoDialogAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, ButtonEnum.Ok, Icon.Info);
        await box.ShowAsync();
    }

    public async Task ShowWarningDialogAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, ButtonEnum.Ok, Icon.Warning);
        await box.ShowAsync();
    }

    public async Task ShowErrorDialogAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, ButtonEnum.Ok, Icon.Error);
        await box.ShowAsync();
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
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, question, ButtonEnum.YesNo, Icon.Question);
        var result = await box.ShowAsync();
        return result == ButtonResult.Yes;
    }

    public async Task<string?> SelectNewFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            throw new NotSupportedException();
        }

        var topLevel = TopLevel.GetTopLevel(desktopApp.MainWindow);
        var suggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedStartLocation = suggestedStartLocation,
            FileTypeChoices = new [] { fileType, },
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
        var suggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            SuggestedStartLocation = suggestedStartLocation,
            AllowMultiple = false,
            FileTypeFilter = new[] { fileType, },
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
        var suggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDirectory);

        var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            SuggestedStartLocation = suggestedStartLocation,
            AllowMultiple = false,
        });

        return files.Count > 0 ? files[0].Path.AbsolutePath : null;
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
