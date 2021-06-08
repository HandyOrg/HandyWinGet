using System.Windows;
using HandyWinget.Common;
using HandyWinget.Views;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;

namespace HandyWinget
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override void InitializeShell(DependencyObject shell)
        {
            base.InitializeShell(shell);
            if (Helper.Settings.IsFirstRun)
            {
                Container.Resolve<IRegionManager>().RequestNavigate("ContentRegion", "GeneralView");
            }
            else
            {
                Container.Resolve<IRegionManager>().RequestNavigate("ContentRegion", "PackageView");
            }
        }
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<CreatePackageView>();
            containerRegistry.RegisterForNavigation<GeneralView>();
            containerRegistry.RegisterForNavigation<PackageView>();
        }
    }
}
