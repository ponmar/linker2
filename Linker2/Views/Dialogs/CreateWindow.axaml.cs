using Avalonia.Controls;
using Linker2.Model;
using Linker2.ViewModels;

namespace Linker2.Views.Dialogs;

public partial class CreateWindow : Window
{
    public CreateWindow()
    {
        InitializeComponent();
        DataContext = ServiceLocator.Resolve<CreateViewModel>();

        this.RegisterForEvent<CloseDialog>((x) => Close());
    }
}
