using Avalonia.Controls;
using Linker2.Model;

namespace Linker2.Views.Dialogs;

public partial class AddOrEditLinkWindow : Window
{
    private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

    public AddOrEditLinkWindow()
    {
        InitializeComponent();

        this.RegisterForEvent<SessionStopped>((x) => Close());
        this.RegisterForEvent<CloseDialog>((x) => Close());
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        sessionUtils.ResetSessionTime();
    }
}
