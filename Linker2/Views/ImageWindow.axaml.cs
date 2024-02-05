using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class ImageWindow : Window
    {
        public ImageWindow(ImageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.RegisterForEvent<SessionStopped>((x) => Close());
        }
    }
}
