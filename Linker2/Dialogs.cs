using FluentValidation.Results;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;

namespace Linker2;

public interface IDialogs
{
    void ShowInfoDialogAsync(string message);
    void ShowWarningDialog(string message);
    void ShowErrorDialog(string message);
    void ShowErrorDialog(IEnumerable<string> messages);
    void ShowErrorDialog(ValidationResult validationResult);
    Task<bool> ShowConfirmDialog(string question);
    string? SelectNewFileDialog(string title, string initialDirectory, string fileExtension, string filter);
    string? BrowseExistingFileDialog(string title, string initialDirectory, string filter);
    string? ShowBrowseExistingDirectoryDialog(string title);
    string? ShowBrowseExistingDirectoryDialog(string title, string initialDirectory);
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

    public string? SelectNewFileDialog(string title, string initialDirectory, string fileExtension, string filter)
    {
        /*
        var dialog = new System.Windows.Forms.OpenFileDialog()
        {
            Title = title,
            DefaultExt = fileExtension,
            Filter = filter,
            InitialDirectory = initialDirectory,
            CheckFileExists = false,
            CheckPathExists = true,
        };

        var result = dialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            if (dialog.FileName.EndsWith(fileExtension))
            {
                return dialog.FileName;
            }
            else
            {
                ShowErrorDialog($"File extension must be {fileExtension}");
            }
        }
        */

        return null;
    }

    public string? BrowseExistingFileDialog(string title, string initialDirectory, string filter)
    {
        /*
        var dialog = new Microsoft.Win32.OpenFileDialog()
        {
            Title = title,
            Filter = filter,
            InitialDirectory = initialDirectory,
            CheckFileExists = true,
            CheckPathExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
        */
        return null;
    }

    public string? ShowBrowseExistingDirectoryDialog(string title)
    {
        return ShowBrowseExistingDirectoryDialog(title, string.Empty);
    }

    public string? ShowBrowseExistingDirectoryDialog(string title, string initialDirectory)
    {
        /*
        if (!initialDirectory.EndsWith(Path.DirectorySeparatorChar))
        {
            initialDirectory += Path.DirectorySeparatorChar;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = title,
            UseDescriptionForTitle = true,
            SelectedPath = initialDirectory,
            ShowNewFolderButton = true
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
        */
        return null;
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
