using HandyWinget.Views;
using ModernWpf.Controls;
using Prism.Regions;
using System.Windows;
using System.Windows.Input;
using static HandyWinget.Common.Helper;
namespace HandyWinget
{
    public partial class MainWindow
    {
        internal static MainWindow Instance;

        IRegionManager _regionManager;
        public MainWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            _regionManager = regionManager;
            Instance = this;

            LoadSettings();
        }

        private void LoadSettings()
        {
            if (Settings.IsFirstRun)
            {
                navView.SelectedItem = navView.MenuItems[0];
                Settings.IsFirstRun = false;
            }

            navView.PaneDisplayMode = Settings.PaneDisplayMode;
            navView.IsBackButtonVisible = Settings.IsBackEnabled ? NavigationViewBackButtonVisible.Visible : NavigationViewBackButtonVisible.Collapsed;
        }

        public void CommandButtonsVisibility(Visibility visibility)
        {
            //appBarInstall.Visibility = visibility;
            //appBarRefresh.Visibility = visibility;
            //appBarIsInstalled.Visibility = visibility;
            //appBarSeperator.Visibility = visibility;
        }
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem) args.SelectedItem;
            if (selectedItem != null)
            {
                Navigate(selectedItem.Tag.ToString());

                switch (selectedItem.Tag)
                {
                    case "CreatePackageView":
                        CommandButtonsVisibility(Visibility.Collapsed);
                        break;
                    case "PackageView":
                        CommandButtonsVisibility(Visibility.Visible);
                        break;
                    case "GeneralView":
                        CommandButtonsVisibility(Visibility.Collapsed);
                        break;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.P)
            {
                OpenTerminal("Powershell");

            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.W)
            {
                OpenTerminal("CMD");

            }
        }

        private void OpenTerminal(string tag)
        {
            switch (tag)
            {
                case "Powershell":
                    System.Diagnostics.Process.Start("powershell.exe");
                    break;
                case "CMD":
                    System.Diagnostics.Process.Start("cmd.exe");
                    break;
            }
        }

        private void OpenTerminal_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is AppBarButton button)
            {
                OpenTerminal(button.Label.ToString());
            }
        }

        private void appBarRefresh_Click(object sender, RoutedEventArgs e)
        {
            Packages.Instance.DownloadManifests(true);
        }

        private void appBarInstall_Click(object sender, RoutedEventArgs e)
        {
            Packages.Instance.InstallPackage();
        }

        private void appBarIsInstalled_Checked(object sender, RoutedEventArgs e)
        {
            //Packages.Instance.FilterInstalledApps(appBarIsInstalled.IsChecked.Value);
        }

        private void OpenFlyout(string resourceKey, FrameworkElement element)
        {
            //var cmdBarFlyout = (CommandBarFlyout)Resources[resourceKey];
            //var paneMode = Settings.PaneDisplayMode;
            //switch (paneMode)
            //{
            //    case NavigationViewPaneDisplayMode.Auto:
            //    case NavigationViewPaneDisplayMode.Left:
            //    case NavigationViewPaneDisplayMode.LeftCompact:
            //    case NavigationViewPaneDisplayMode.LeftMinimal:
            //        cmdBarFlyout.Placement = FlyoutPlacementMode.RightEdgeAlignedTop;
            //        break;
            //    case NavigationViewPaneDisplayMode.Top:
            //        cmdBarFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
            //        break;
            //}

            //cmdBarFlyout.ShowAt(element);
        }

        private void nvOpenTerminal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFlyout("TerminalCommandBar", nvOpenTerminal);
        }

        private void navView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            
        }

        private void Navigate(string view)
        {
            _regionManager.RequestNavigate("ContentRegion", view);
        }
    }
}
