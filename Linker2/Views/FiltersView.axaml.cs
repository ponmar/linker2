using Avalonia.Controls;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class FiltersView : UserControl
    {
        public FiltersView()
        {
            InitializeComponent();
            DataContext = ServiceLocator.Resolve<LinksViewModel>();
        }
    }
}
