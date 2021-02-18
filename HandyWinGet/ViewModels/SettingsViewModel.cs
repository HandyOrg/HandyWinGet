using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Views;
using ModernWpf.Controls;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace HandyWinGet.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        #region Command
        private DelegateCommand _checkUpdateCommand;
        private DelegateCommand<SelectionChangedEventArgs> _identifyPackageModeChangedCommand;
        private DelegateCommand<SelectionChangedEventArgs> _installModeChangedCommand;
        private DelegateCommand<SelectionChangedEventArgs> _paneDisplayModeChangedCommand;

        public DelegateCommand CheckUpdateCommand =>
            _checkUpdateCommand ??= new DelegateCommand(OnCheckUpdate);

        public DelegateCommand<SelectionChangedEventArgs> PaneDisplayModeChangedCommand =>
            _paneDisplayModeChangedCommand ??= new DelegateCommand<SelectionChangedEventArgs>(OnPaneDisplayModeChanged);

        public DelegateCommand<SelectionChangedEventArgs> InstallModeChangedCommand =>
            _installModeChangedCommand ??= new DelegateCommand<SelectionChangedEventArgs>(OnInstallModeChanged);

        public DelegateCommand<SelectionChangedEventArgs> IdentifyPackageModeChangedCommand =>
            _identifyPackageModeChangedCommand ??= new DelegateCommand<SelectionChangedEventArgs>(OnIdentifyPackageModeChanged);
        #endregion

        #region Property

        private int _identifyPackageModeIndex;
        private int _installModeIndex;
        private bool _isIdmEnabled;
        private bool _isShowingExtraDetail;
        private bool _isShowingGroup;
        private bool _isVisibleIdm;
        private int _paneIndex;
        private string _version;

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public int PaneIndex
        {
            get => _paneIndex;
            set => SetProperty(ref _paneIndex, value);
        }

        public int InstallModeIndex
        {
            get => _installModeIndex;
            set => SetProperty(ref _installModeIndex, value);
        }

        public int IdentifyPackageModeIndex
        {
            get => _identifyPackageModeIndex;
            set => SetProperty(ref _identifyPackageModeIndex, value);
        }

        public bool IsIdmEnabled
        {
            get => _isIdmEnabled;
            set
            {
                SetProperty(ref _isIdmEnabled, value);
                GlobalDataHelper<AppConfig>.Config.IsIDMEnabled = value;
                GlobalDataHelper<AppConfig>.Save();
                GlobalDataHelper<AppConfig>.Init();
            }
        }

        public bool IsVisibleIdm
        {
            get => _isVisibleIdm;
            set => SetProperty(ref _isVisibleIdm, value);
        }

        public bool IsShowingGroup
        {
            get => _isShowingGroup;
            set
            {
                SetProperty(ref _isShowingGroup, value);
                GlobalDataHelper<AppConfig>.Config.IsShowingGroup = value;
                GlobalDataHelper<AppConfig>.Save();
                GlobalDataHelper<AppConfig>.Init();
                PackagesViewModel.Instance.SetDataGridGrouping();
                Packages.Instance.SetPublisherVisibility();
            }
        }

        public bool IsShowingExtraDetail
        {
            get => _isShowingExtraDetail;
            set
            {
                SetProperty(ref _isShowingExtraDetail, value);
                GlobalDataHelper<AppConfig>.Config.IsShowingExtraDetail = value;
                GlobalDataHelper<AppConfig>.Save();
                GlobalDataHelper<AppConfig>.Init();
                PackagesViewModel.Instance.RowDetailsVisibilityMode = value
                    ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                    : DataGridRowDetailsVisibilityMode.Collapsed;
            }
        }
        #endregion

        public SettingsViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            PaneIndex = (int)GlobalDataHelper<AppConfig>.Config.PaneDisplayMode;
            InstallModeIndex = (int)GlobalDataHelper<AppConfig>.Config.InstallMode;
            IsVisibleIdm = InstallModeIndex == (int)InstallMode.Internal;
            IdentifyPackageModeIndex = (int)GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode;

            IsIdmEnabled = GlobalDataHelper<AppConfig>.Config.IsIDMEnabled;
            IsShowingGroup = GlobalDataHelper<AppConfig>.Config.IsShowingGroup;
            IsShowingExtraDetail = GlobalDataHelper<AppConfig>.Config.IsShowingExtraDetail;
        }

        private void OnPaneDisplayModeChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            if (e.AddedItems[0] is NavigationViewPaneDisplayMode item)
            {
                if (!item.Equals(GlobalDataHelper<AppConfig>.Config.PaneDisplayMode))
                {
                    GlobalDataHelper<AppConfig>.Config.PaneDisplayMode = item;
                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                    MainWindowViewModel.Instance.PaneDisplayMode = item;
                }
            }
        }

        private void OnInstallModeChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            if (e.AddedItems[0] is InstallMode item)
            {
                if (!item.Equals(GlobalDataHelper<AppConfig>.Config.InstallMode))
                {
                    GlobalDataHelper<AppConfig>.Config.InstallMode = item;

                    if (item.Equals(InstallMode.Internal))
                    {
                        IsVisibleIdm = true;
                    }
                    else
                    {
                        if (!OSVersionHelper.IsWindows10_1709_OrGreater)
                        {
                            MessageBox.Error(
                                "Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later",
                                "OS is not Supported");
                            InstallModeIndex = 1;
                        }
                        else
                        {
                            IsVisibleIdm = false;
                        }
                    }

                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                }
            }
        }

        private void OnIdentifyPackageModeChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            if (e.AddedItems[0] is IdentifyPackageMode item)
            {
                if (!item.Equals(GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode))
                {
                    GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode = item;

                    if (item.Equals(IdentifyPackageMode.Wingetcli))
                    {
                        if (!OSVersionHelper.IsWindows10_1709_OrGreater)
                        {
                            MessageBox.Error(
                                "Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later",
                                "OS is not Supported");
                            IdentifyPackageModeIndex = 0;
                        }
                    }

                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                }
            }
        }

        private void OnCheckUpdate()
        {
            try
            {
                var ver =
                    UpdateHelper.Instance.CheckUpdate("HandyOrg", "HandyWinGet");

                if (ver.IsExistNewVersion)
                {
                    Growl.AskGlobal("we found a new Version, do you want to download?", b =>
                    {
                        if (!b)
                        {
                            return true;
                        }

                        var exeLocation = Environment.CurrentDirectory + @"\HandyWinGet.exe";

                        Process.Start(
                            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) +
                            @"\HandyUpdater.exe",
                            $"{Version} {ver.TagName.Replace("v", "")} {Environment.CurrentDirectory} {exeLocation} {ver.Asset[0].browser_download_url} ");
                        Environment.Exit(0);
                        return true;
                    });
                }
                else
                {
                    Growl.InfoGlobal("you are using Latest Version.");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }
    }
}