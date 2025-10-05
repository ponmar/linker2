using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linker2.Model;
using System;
using Linker2.Validators;
using Linker2.Cryptography;
using System.Threading.Tasks;

namespace Linker2.ViewModels.Dialogs;

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
    private async Task SaveAsync()
    {
        try
        {
            sessionUtils.ChangePassword(AesUtils.StringToSecureString(CurrentPassword), AesUtils.StringToSecureString(NewPassword));
            Messenger.Send<CloseDialog>();
        }
        catch (ValidationException e)
        {
            await dialogs.ShowErrorDialogAsync(e.Result);
        }
        catch (Exception e)
        {
            await dialogs.ShowErrorDialogAsync(e.Message);
        }        
    }
}
