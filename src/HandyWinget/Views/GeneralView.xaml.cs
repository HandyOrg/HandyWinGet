using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Themes;
using HandyControl.Tools;
using HandyWinget.Common;
using HandyWinget.Control;
using ModernWpf.Controls;
using static HandyWinget.Common.Helper;
namespace HandyWinget.Views
{
    public partial class GeneralView : UserControl
    {
        private readonly List<string> _colorPresetList = new List<string>
        {
            "#f44336",
            "#e91e63",
            "#9c27b0",
            "#673ab7",
            "#3f51b5",
            "#2196f3",
            "#03a9f4",
            "#00bcd4",
            "#009688",

            "#4caf50",
            "#8bc34a",
            "#cddc39",
            "#ffeb3b",
            "#ffc107",
            "#ff9800",
            "#ff5722",
            "#795548",
            "#9e9e9e"
        };
        string currentVersion = string.Empty;

        public GeneralView()
        {
            InitializeComponent();
            LoadInitialSettings();
            InitAccentButtons();
        }
        private void LoadInitialSettings()
        {
            MainWindow.Instance.navView.PaneDisplayMode = Settings.PaneDisplayMode;
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            txtCurrentVersion.Text = $"Current Version {currentVersion}";
            cmbPaneDisplay.SelectedItem = Settings.PaneDisplayMode;
            cmbInstall.SelectedItem = Settings.InstallMode;

            switch (Settings.InstallMode)
            {
                case InstallMode.Wingetcli:
                    stkIDM.Visibility = Visibility.Collapsed;
                    break;
                case InstallMode.Internal:
                    stkIDM.Visibility = Visibility.Visible;
                    break;
            }

            tgIDM.IsChecked = Settings.IsIDMEnabled;
            tgGroup.IsChecked = Settings.GroupByPublisher;
            tgSaveDGColumnWidth.IsChecked = Settings.IsStoreDataGridColumnWidth;
            tgAutoRefresh.IsChecked = Settings.AutoRefreshInStartup;
            tgIdentify.IsChecked = Settings.IdentifyInstalledPackage;

            if (Settings.Theme == ApplicationTheme.Light)
            {
                radioButtons.SelectedIndex = 0;
            }
            else
            {
                radioButtons.SelectedIndex = 1;
            }
        }

        #region Settings
        #region ComboBox
        private void cmbInstall_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = (InstallMode) cmbInstall.SelectedItem;
            switch (mode)
            {
                case InstallMode.Wingetcli:
                    stkIDM.Visibility = Visibility.Collapsed;
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
                    stkIDM.Visibility = Visibility.Visible;
                    break;
            }

            if (mode != Settings.InstallMode)
            {
                Settings.InstallMode = mode;
            }
        }

        #endregion

        #region ToggleButtons
        private void tgIDM_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            var state = tgIDM.IsChecked.Value;
            if (state != Settings.IsIDMEnabled)
            {
                Settings.IsIDMEnabled = state;
            }
        }

        private void tgIdentify_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsOsSupported())
            {
                tgIdentify.IsChecked = false;
                CreateInfoBar("OS Not Supported", "Your operating system does not support this feature", panel, Severity.Error);
            }
            var state = tgIdentify.IsChecked.Value;
            if (state != Settings.IdentifyInstalledPackage)
            {
                Settings.IdentifyInstalledPackage = state;
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
        private void tgAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            var state = tgAutoRefresh.IsChecked.Value;
            if (state != Settings.AutoRefreshInStartup)
            {
                Settings.AutoRefreshInStartup = state;
            }
        }

        private void tgSaveDGColumnWidth_Checked(object sender, RoutedEventArgs e)
        {
            var state = tgSaveDGColumnWidth.IsChecked.Value;
            if (state != Settings.IsStoreDataGridColumnWidth)
            {
                Settings.IsStoreDataGridColumnWidth = state;
            }
        }

        #endregion
        #endregion

        #region Appearance

        #region Accent Button
        private void InitAccentButtons()
        {
            foreach (var item in _colorPresetList)
            {
                panelColor.Children.Add(CreateColorButton(item));
            }
        }

        private Button CreateColorButton(string colorStr)
        {
            var color = ColorConverter.ConvertFromString(colorStr) ?? default(Color);
            var brush = new SolidColorBrush((Color) color);

            var button = new Button
            {
                Margin = new Thickness(6),
                Style = ResourceHelper.GetResource<Style>("InfoBarCloseButtonStyle"),
                Background = brush,
                Height = 40,
                Width = 40
            };

            button.Click += (s, e) =>
            {
                ((App) Application.Current).UpdateAccent(brush);
                Settings.Accent = brush;
            };

            return button;
        }

        #endregion
        private void cmbPaneDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = (NavigationViewPaneDisplayMode) cmbPaneDisplay.SelectedItem;
            if (mode != Settings.PaneDisplayMode)
            {
                Settings.PaneDisplayMode = mode;
                MainWindow.Instance.navView.PaneDisplayMode = mode;
            }
        }
        private void OnTheme_Checked(object sender, RoutedEventArgs e)
        {
            var selectedTheme = ((RadioButton) sender)?.Tag?.ToString();
            if (selectedTheme != null)
            {
                var value = ParseEnum<ApplicationTheme>(selectedTheme);

                if (value != Settings.Theme)
                {
                    ((App) Application.Current).UpdateTheme(value);
                    Settings.Theme = value;
                }
            }
        }
        private void ResetAccent_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Settings.Accent = null;
            ((App) Application.Current).UpdateAccent(null);
        }

        private void ChooseAccent_Click(object sender, RoutedEventArgs e)
        {
            CreateColorPicker();
        }
        #endregion

        #region About
        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCheck.IsEnabled = false;
                var version = await UpdateHelper.CheckUpdateAsync("HandyOrg", "HandyWinGet");

                if (version.IsExistNewVersion)
                {
                    CreateInfoBarWithAction("New Update Available", $"we found a new Version {version.TagName}, do you want to download?", panel, Severity.Success, "Download New Version", () =>
                    {
                        StartProcess(version.Assets[0].Url);
                    });
                }
                else
                {
                    CreateInfoBar("Latest Version", "you are using Latest Version.", panel, Severity.Error);
                }

                btnCheck.IsEnabled = true;

            }
            catch (Exception ex)
            {
                CreateInfoBar("Error", ex.Message, panel, Severity.Error);
            }
        }
        #endregion

    }
}

