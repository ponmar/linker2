using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Configuration;
using Linker2.Cryptography;
using Linker2.Model;
using Linker2.Validators;
using System;
using System.IO;
using System.IO.Abstractions;

namespace Linker2.ViewModels;

public partial class CreateViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string filename = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string password = string.Empty;

    public bool CanCreate =>
        !string.IsNullOrEmpty(Filename) &&
        passwordValidator.Validate(AesUtils.StringToSecureString(Password)).IsValid;

    private readonly IFileSystem fileSystem;
    private readonly IDialogs dialogs;
    private readonly IFileUtils fileUtils;

    private readonly PasswordValidator passwordValidator = new();

    public CreateViewModel(IFileSystem fileSystem, IDialogs dialogs, IFileUtils fileUtils)
    {
        this.fileSystem = fileSystem;
        this.dialogs = dialogs;
        this.fileUtils = fileUtils;
    }

    [RelayCommand]
    private void Browse()
    {
        var initialDirectory = EncryptedApplicationConfig<DataDto>.GetDirectory(Constants.AppName);
        string? filePath = dialogs.SelectNewFileDialog("New File", initialDirectory, ".linker", "Linker documents (.linker)|*.linker");
        if (filePath is null)
        {
            return;
        }

        if (fileSystem.File.Exists(filePath))
        {
            dialogs.ShowErrorDialog("File already exists");
            return;
        }

        Filename = Path.GetFileName(filePath);
    }

    [RelayCommand]
    private void Create()
    {
        try
        {
            fileUtils.Create(Filename, AesUtils.StringToSecureString(Password));
            Messenger.Send(new CloseDialog());
        }
        catch (ValidationException e)
        {
            dialogs.ShowErrorDialog(e.Result);
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialog($"Unable to create {Filename}: {e.Message}");
        }
    }
}
