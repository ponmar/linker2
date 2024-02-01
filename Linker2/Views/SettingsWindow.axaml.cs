using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;
using System.IO.Abstractions;

namespace Linker2.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsDto settings)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(ServiceLocator.Resolve<IFileSystem>(), ServiceLocator.Resolve<IDialogs>(), ServiceLocator.Resolve<ISessionSaver>(), settings);

            this.RegisterForEvent<SessionStopped>((x) => Close());
            this.RegisterForEvent<CloseDialog>((x) => Close());
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
