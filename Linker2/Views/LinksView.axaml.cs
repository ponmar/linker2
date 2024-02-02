using Avalonia.Controls;
using Linker2.ViewModels;

namespace Linker2.Views;

public partial class LinksView : UserControl
{
    public LinksView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.Resolve<LinksViewModel>();
    }

    private void Grid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var grid = sender as Grid;
        var linksViewModel = grid!.DataContext as LinkViewModel;
        linksViewModel!.OpenLinkCommand.Execute(linksViewModel.LinkDto);
    }
}
