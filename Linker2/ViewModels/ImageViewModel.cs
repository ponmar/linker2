using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Linker2.ViewModels;

public partial class ImageViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageWidth))]
    [NotifyPropertyChangedFor(nameof(ImageHeight))]
    private Bitmap? imageBitmap;

    public int ImageWidth => ImageBitmap is null ? 0 : ImageBitmap.PixelSize.Width;
    public int ImageHeight => ImageBitmap is null ? 0 : ImageBitmap.PixelSize.Height;
}
