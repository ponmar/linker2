using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Model;
using System;
using Linker2.Validators;
using Linker2.Cryptography;

namespace Linker2.ViewModels;

public partial class PasswordViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string newPassword = string.Empty;

    public bool CanSave => CurrentPassword.Length > 0 && NewPassword.Length > 0;

    private readonly ISessionUtils sessionUtils;
    private readonly IDialogs dialogs;

    public PasswordViewModel(ISessionUtils sessionUtils, IDialogs dialogs)
    {
        this.sessionUtils = sessionUtils;
        this.dialogs = dialogs;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            sessionUtils.ChangePassword(AesUtils.StringToSecureString(CurrentPassword), AesUtils.StringToSecureString(NewPassword));
            Messenger.Send(new CloseDialog());
        }
        catch (ValidationException e)
        {
            dialogs.ShowErrorDialog(e.Result);
        }
        catch (Exception e)
        {
            dialogs.ShowErrorDialog(e.Message);
        }        
    }
}
