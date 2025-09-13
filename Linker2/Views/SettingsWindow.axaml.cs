using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = ServiceLocator.Resolve<SettingsViewModel>();

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
}
