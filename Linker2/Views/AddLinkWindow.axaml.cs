using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class AddLinkWindow : Window
    {
        public AddLinkWindow(AddLinkViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;

            // TODO
            //UrlTextBox.Focus();

            this.RegisterForEvent<SessionStopped>((x) => Close());
            this.RegisterForEvent<CloseDialog>((x) => Close());
        }
    }
}
