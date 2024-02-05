using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class ImageWindow : Window
    {
        private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

        public ImageWindow(ImageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.RegisterForEvent<SessionStopped>((x) => Close());
        }

        private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            sessionUtils.ResetSessionTime();
        }
    }
}
