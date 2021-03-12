using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using HandyWinget.Views;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static HandyWinget.Assets.Helper;
namespace HandyWinget
{
    public partial class MainWindow
    {
        internal static MainWindow Instance;

        public MainWindow()
        {
            InitializeComponent();
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
        }

        public void CommandButtonsVisibility(Visibility visibility)
        {
            appBarInstall.Visibility = visibility;
            appBarRefresh.Visibility = visibility;
            appBarExport.Visibility = visibility;
            appBarIsInstalled.Visibility = visibility;
            appBarSeperator.Visibility = visibility;
        }
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            if (selectedItem != null)
            {
                switch (selectedItem.Tag)
                {
                    case "CreatePackage":
                        CommandButtonsVisibility(Visibility.Collapsed);
                        contentFrame?.Navigate(typeof(CreatePackage));
                        break;
                    case "Packages":
                        CommandButtonsVisibility(Visibility.Visible);
                        contentFrame?.Navigate(typeof(Packages));
                        break;
                    case "General":
                        CommandButtonsVisibility(Visibility.Collapsed);
                        contentFrame?.Navigate(typeof(General));
                        break;
                }
            }
        }

        private void ApplicationTheme_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is AppBarButton button && button.Tag is ApplicationTheme tag)
            {
                if (tag.Equals(Settings.Theme)) return;

                Settings.Theme = tag;
                ((App)Application.Current).UpdateTheme(tag);
            }
            else if (e.OriginalSource is AppBarButton btn && (string)btn.Tag is "Accent")
            {
                var picker = SingleOpenHelper.CreateControl<ColorPicker>();
                var window = new PopupWindow
                {
                    PopupElement = picker,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    AllowsTransparency = true,
                    WindowStyle = WindowStyle.None,
                    MinWidth = 0,
                    MinHeight = 0,
                    Title = "Accent Color"
                };

                if (Settings.Accent !=null)
                {
                    picker.SelectedBrush = new SolidColorBrush(ApplicationHelper.GetColorFromBrush(Settings.Accent));
                }

                picker.SelectedColorChanged += delegate
                {
                    ((App)Application.Current).UpdateAccent(picker.SelectedBrush);
                    Settings.Accent = picker.SelectedBrush;
                    window.Close();
                };
                picker.Canceled += delegate { window.Close(); };
                window.Show();
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

        private void appBarExport_Click(object sender, RoutedEventArgs e)
        {
            Packages.Instance.ExportPowerShellScript();
        }

        private void appBarInstall_Click(object sender, RoutedEventArgs e)
        {
            Packages.Instance.InstallPackage();
        }

        private void appBarIsInstalled_Checked(object sender, RoutedEventArgs e)
        {
            Packages.Instance.FilterInstalledApps(appBarIsInstalled.IsChecked.Value);
        }

        private void OpenFlyout(string resourceKey, FrameworkElement element)
        {
            var cmdBarFlyout = (CommandBarFlyout)Resources[resourceKey];
            var paneMode = Settings.PaneDisplayMode;
            switch (paneMode)
            {
                case NavigationViewPaneDisplayMode.Auto:
                case NavigationViewPaneDisplayMode.Left:
                case NavigationViewPaneDisplayMode.LeftCompact:
                case NavigationViewPaneDisplayMode.LeftMinimal:
                    cmdBarFlyout.Placement = FlyoutPlacementMode.RightEdgeAlignedTop;
                    break;
                case NavigationViewPaneDisplayMode.Top:
                    cmdBarFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
                    break;
            }
            
            cmdBarFlyout.ShowAt(element);
        }

        private void nvChangeTheme_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFlyout("ThemeCommandBar", nvChangeTheme);
        }

        private void nvOpenTerminal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFlyout("TerminalCommandBar", nvOpenTerminal);
        }
    }
}
