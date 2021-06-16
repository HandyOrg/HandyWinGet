using System.Windows;
using HandyWinget.Common;
using HandyWinget.Views;
using ModernWpf.Controls;
using Prism.DryIoc;
using Prism.Ioc;

namespace HandyWinget
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override void InitializeShell(DependencyObject shell)
        {
            base.InitializeShell(shell);
            if (Helper.Settings.IsFirstRun)
            {
                MainWindow.Instance.navView.SelectedItem = MainWindow.Instance.navView.MenuItems[0] as NavigationViewItem;
            }
            else
            {
                MainWindow.Instance.navView.SelectedItem = MainWindow.Instance.navView.MenuItems[1] as NavigationViewItem;
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
