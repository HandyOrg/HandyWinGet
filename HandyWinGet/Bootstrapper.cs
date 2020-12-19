using System.Windows;
using HandyWinGet.Views;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;

namespace HandyWinGet
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override void InitializeShell(DependencyObject shell)
        {
            base.InitializeShell(shell);
            Container.Resolve<IRegionManager>().RequestNavigate("ContentRegion", "Packages");
        }

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<CreatePackage>();
            containerRegistry.RegisterForNavigation<Packages>();
            containerRegistry.RegisterForNavigation<Settings>();
            containerRegistry.RegisterForNavigation<PackageManager>();
        }
    }
}