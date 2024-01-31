using Avalonia.Controls;
using Linker2.ViewModels;

namespace Linker2.Views;

public partial class FileView : UserControl
{
    public FileView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.Resolve<MainViewModel>();
    }
}
