using Linker2.Model;
using Linker2.ViewModels;
using Linker2.ViewModels.Dialogs;
using System.IO.Abstractions;

namespace Linker2;

public static class Bootstrapper
{ 
    public static void Bootstrap()
    {
        ServiceLocator.RegisterSingleton<IFileSystem, FileSystem>();
        ServiceLocator.RegisterSingleton<IFileUtils, FileUtils>();
        ServiceLocator.RegisterSingleton<IDialogs, Dialogs>();

        ServiceLocator.RegisterSingleton<Model.Model>([
            typeof(ILinkRepository),
            typeof(ISettingsProvider),
            typeof(ILinkModification),
            typeof(ISessionSaver),
            typeof(ISessionUtils),
            typeof(IWebPageScraperProvider)]);

        ServiceLocator.RegisterSingleton<IClipboardService, ClipboardService>();
        ServiceLocator.RegisterSingleton<ILinkFileRepository, LinkFileRepository>();

        ServiceLocator.RegisterTransient<AddOrEditLinkViewModel>();
        ServiceLocator.RegisterTransient<CreateViewModel>();
        ServiceLocator.RegisterTransient<PasswordViewModel>();
        ServiceLocator.RegisterTransient<ImageViewModel>();
        ServiceLocator.RegisterTransient<SettingsViewModel>();

        ServiceLocator.RegisterSingleton<MainViewModel>();
        ServiceLocator.RegisterSingleton<LinksViewModel>();
    }
}
