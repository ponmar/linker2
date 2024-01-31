using Avalonia.Controls;
using Linker2.ViewModels;

namespace Linker2.Views
{
    public partial class LinksView : UserControl
    {
        public LinksView()
        {
            InitializeComponent();
            DataContext = ServiceLocator.Resolve<LinksViewModel>();
        }
    }
}
