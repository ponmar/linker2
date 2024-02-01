using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class PasswordWindow : Window
    {
        public PasswordWindow()
        {
            InitializeComponent();
            DataContext = ServiceLocator.Resolve<PasswordViewModel>();

            this.RegisterForEvent<SessionStopped>((x) => Close());
            this.RegisterForEvent<CloseDialog>((x) => Close());
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
