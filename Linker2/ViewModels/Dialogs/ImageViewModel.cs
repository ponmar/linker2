using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Linker2.ViewModels.Dialogs;

public partial class ImageViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageWidth))]
    [NotifyPropertyChangedFor(nameof(ImageHeight))]
    public partial Bitmap? ImageBitmap { get; set; }

    public int ImageWidth => ImageBitmap is null ? 0 : ImageBitmap.PixelSize.Width;
    public int ImageHeight => ImageBitmap is null ? 0 : ImageBitmap.PixelSize.Height;
}
