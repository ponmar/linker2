using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views.Dialogs;

public partial class ImageWindow : Window
{
    private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

    private readonly ImageViewModel viewModel;

    public ImageWindow()
    {
        InitializeComponent();
        viewModel = ServiceLocator.Resolve<ImageViewModel>();
        DataContext = viewModel;
        this.RegisterForEvent<SessionStopped>((x) => Close());
    }

    public void SetImage(Bitmap image)
    {
        viewModel.ImageBitmap = image;
    }

    private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        sessionUtils.ResetSessionTime();
    }
}
