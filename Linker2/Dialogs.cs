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
    void ShowInfoDialogAsync(string message);
    void ShowWarningDialog(string message);
    void ShowErrorDialog(string message);
    void ShowErrorDialog(IEnumerable<string> messages);
    void ShowErrorDialog(ValidationResult validationResult);
    Task<bool> ShowConfirmDialog(string question);
    Task<string?> SelectNewFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType);
    Task<string?> BrowseExistingFileDialogAsync(string title, string initialDirectory, FilePickerFileType fileType);
    Task<string?> ShowBrowseExistingDirectoryDialogAsync(string title);
    Task<string?> ShowBrowseExistingDirectoryDialogAsync(string title, string initialDirectory);
    void OpenUrlInDefaultBrowser(string url);
}

public class Dialogs : IDialogs
{
    public async void ShowInfoDialogAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, ButtonEnum.Ok, Icon.Info);
        var result = await box.ShowAsync();
    }

    public async void ShowWarningDialog(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, ButtonEnum.Ok, Icon.Warning);
        var result = await box.ShowAsync();
    }

    public async void ShowErrorDialog(string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(Constants.AppName, message, ButtonEnum.Ok, Icon.Error);
        var result = await box.ShowAsync();
    }

    public void ShowErrorDialog(IEnumerable<string> messages)
    {
        ShowErrorDialog(string.Join("\n", messages));
    }

    public void ShowErrorDialog(ValidationResult validationResult)
    {
        ShowErrorDialog(validationResult.Errors.Select(x => x.ErrorMessage));
    }

    public async Task<bool> ShowConfirmDialog(string question)
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
