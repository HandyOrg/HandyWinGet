using HandyControl.Controls;
using HandyControl.Tools;
using HandyWinget.Assets;
using ModernWpf.Controls;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using static HandyWinget.Assets.Helper;
namespace HandyWinget.Views
{
    /// <summary>
    /// Interaction logic for General
    /// </summary>
    public partial class General : UserControl
    {
        string Version = string.Empty;
        public General()
        {
            InitializeComponent();
            LoadInitialSettings();
        }

        private void LoadInitialSettings()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            cmbPaneDisplay.SelectedItem = Settings.PaneDisplayMode;
            MainWindow.Instance.navView.PaneDisplayMode = Settings.PaneDisplayMode;
            cmbIdentify.SelectedItem = Settings.IdentifyPackageMode;
            cmbInstall.SelectedItem = Settings.InstallMode;

            switch (Settings.InstallMode)
            {
                case InstallMode.Wingetcli:
                    tgIDM.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case InstallMode.Internal:
                    tgIDM.Visibility = System.Windows.Visibility.Visible;
                    break;
            }

            tgIDM.IsChecked = Settings.IsIDMEnabled;
            tgGroup.IsChecked = Settings.GroupByPublisher;
            cmbDetails.SelectedItem = Settings.ShowExtraDetails;
            currentVersion.Text = $"Current Version {Version}";
        }

        private void cmbPaneDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = (NavigationViewPaneDisplayMode)cmbPaneDisplay.SelectedItem;
            if (mode != Settings.PaneDisplayMode)
            {
                Settings.PaneDisplayMode = mode;
                MainWindow.Instance.navView.PaneDisplayMode = mode;
            }
        }

        private void cmbIdentify_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = (IdentifyPackageMode)cmbIdentify.SelectedItem;

            if (!IsOsSupported())
            {
                cmbIdentify.SelectedIndex = 0;
            }

            if (mode != Settings.IdentifyPackageMode)
            {
                Settings.IdentifyPackageMode = mode;
            }
        }

        private void cmbInstall_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = (InstallMode)cmbInstall.SelectedItem;
            switch (mode)
            {
                case InstallMode.Wingetcli:
                    tgIDM.Visibility = Visibility.Collapsed;
                    if (!IsOsSupported())
                    {
                        cmbInstall.SelectedIndex = 1;
                    }

                    if (!IsWingetInstalled())
                    {
                        cmbInstall.SelectedIndex = 1;
                    }
                    break;
                case InstallMode.Internal:
                    tgIDM.Visibility = Visibility.Visible;
                    break;
            }

            if (mode != Settings.InstallMode)
            {
                Settings.InstallMode = mode;
            }
        }

        private void tgIDM_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = tgIDM.IsChecked.Value;
            if (state != Settings.IsIDMEnabled)
            {
                Settings.IsIDMEnabled = state;
            }
        }

        private void ResetAccent_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Settings.Accent = null;
            ((App)Application.Current).UpdateAccent(null);
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCheck.IsEnabled = false;
                var ver = await UpdateHelper.CheckUpdateAsync("HandyOrg", "HandyWinGet");

                if (ver.IsExistNewVersion)
                {
                    Growl.AskGlobal("we found a new Version, do you want to download?", b =>
                    {
                        if (!b)
                        {
                            return true;
                        }
                        StartProcess(ver.Assets[0].Url);
                        return true;
                    });
                }
                else
                {
                    Growl.InfoGlobal("you are using Latest Version.");
                }

                btnCheck.IsEnabled = true;

            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        private void tgGroup_Checked(object sender, RoutedEventArgs e)
        {
            var state = tgGroup.IsChecked.Value;
            if (state != Settings.GroupByPublisher)
            {
                Settings.GroupByPublisher = state;
            }
        }

        private void cmbDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = (DataGridRowDetailsVisibilityMode)cmbDetails.SelectedItem;
            if (mode != Settings.ShowExtraDetails)
            {
                Settings.ShowExtraDetails = mode;
            }
        }
    }
}
