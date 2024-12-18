﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Linker2.Views;
using System.Diagnostics;
using System.Linq;

namespace Linker2;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        Process proc = Process.GetCurrentProcess();
        var count = Process.GetProcesses().Where(p => p.ProcessName == proc.ProcessName).Count();

        Bootstrapper.Bootstrap();

        if (count > 1)
        {
            var dialogs = ServiceLocator.Resolve<IDialogs>();
            dialogs.ShowErrorDialogAsync("This application is already running...");
        }
        else
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
