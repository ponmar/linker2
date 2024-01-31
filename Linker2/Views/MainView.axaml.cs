using Avalonia.Controls;
using Linker2.ViewModels;

namespace Linker2.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.Resolve<MainViewModel>();
    }
}
