using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;
using System.IO.Abstractions;

namespace Linker2.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

        public SettingsWindow(SettingsDto settings)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(ServiceLocator.Resolve<IFileSystem>(), ServiceLocator.Resolve<IDialogs>(), ServiceLocator.Resolve<ISessionSaver>(), settings);

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
