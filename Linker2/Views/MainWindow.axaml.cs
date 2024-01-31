using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views;

public partial class MainWindow : Window
{
    private readonly ISessionUtils sessionUtils = ServiceLocator.Resolve<ISessionUtils>();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = ServiceLocator.Resolve<MainViewModel>();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        sessionUtils.StopSession();
    }
}
