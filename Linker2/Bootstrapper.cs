using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Linker2.Model;
using Linker2.ViewModels;
using System.IO.Abstractions;

namespace Linker2;

public static class Bootstrapper
{
    public static WindsorContainer Container { get; } = new WindsorContainer();

    public static void Bootstrap()
    {
        Container.Register(Component.For<IFileSystem>().ImplementedBy<FileSystem>());
        Container.Register(Component.For<IFileUtils>().ImplementedBy<FileUtils>());
        Container.Register(Component.For<IDialogs>().ImplementedBy<Dialogs>());
        Container.Register(Component.For(
            typeof(ILinkRepository),
            typeof(ISettingsRepository),
            typeof(ILinkModification),
            typeof(ISessionSaver),
            typeof(ISessionUtils),
            typeof(IUrlDataFetcher)).ImplementedBy<Model.Model>());

        //Container.Register(Component.For<AddLinkViewModel>().ImplementedBy<AddLinkViewModel>().LifestyleTransient());
        //Container.Register(Component.For<CreateViewModel>().ImplementedBy<CreateViewModel>().LifestyleTransient());
        //Container.Register(Component.For<PasswordViewModel>().ImplementedBy<PasswordViewModel>().LifestyleTransient());
        Container.Register(Component.For<MainViewModel>().ImplementedBy<MainViewModel>());
        Container.Register(Component.For<LinksViewModel>().ImplementedBy<LinksViewModel>());
    }
}
