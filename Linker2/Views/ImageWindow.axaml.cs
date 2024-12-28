using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class ImageWindow : Window
    {
        private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

        public ImageWindow()
        {
            InitializeComponent();
            DataContext = ServiceLocator.Resolve<ImageViewModel>();
            this.RegisterForEvent<SessionStopped>((x) => Close());
        }

        public void SetImage(Bitmap image)
        {
            ((ImageViewModel)DataContext!).ImageBitmap = image;
        }

        private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            sessionUtils.ResetSessionTime();
        }
    }
}
