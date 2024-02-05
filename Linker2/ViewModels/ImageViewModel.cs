using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Linker2.ViewModels;

public partial class ImageViewModel : ObservableObject
{
    [ObservableProperty]
    private Bitmap imageBitmap;

    public int ImageWidth => ImageBitmap.PixelSize.Width;
    public int ImageHeight => ImageBitmap.PixelSize.Height;

    public ImageViewModel(Bitmap image)
    {
        ImageBitmap = image;
    }
}
