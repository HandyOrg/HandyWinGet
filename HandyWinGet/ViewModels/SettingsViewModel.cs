using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Views;
using Microsoft.Win32;
using ModernWpf.Controls;
using Prism.Commands;
using Prism.Mvvm;

namespace HandyWinGet.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private DelegateCommand _CheckUpdateCommand;

        private DelegateCommand<SelectionChangedEventArgs> _IdentifyPackageModeChangedCommand;

        private int _IdentifyPackageModeIndex;

        private DelegateCommand<SelectionChangedEventArgs> _InstallModeChangedCommand;

        private int _InstallModeIndex;

        private bool _isIdmEnabled;

        private bool _isShowingExtraDetail;

        private bool _isShowingGroup;

        private bool _IsVisibleIDM;

        private DelegateCommand<SelectionChangedEventArgs> _PaneDisplayModeChangedCommand;

        private int _PaneIndex;

        private string _version;

        public SettingsViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            PaneIndex = (int) GlobalDataHelper<AppConfig>.Config.PaneDisplayMode;
            InstallModeIndex = (int) GlobalDataHelper<AppConfig>.Config.InstallMode;
            IsVisibleIDM = InstallModeIndex == (int) InstallMode.Internal;
            IdentifyPackageModeIndex = (int) GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode;

            IsIDMEnabled = GlobalDataHelper<AppConfig>.Config.IsIDMEnabled;
            IsShowingGroup = GlobalDataHelper<AppConfig>.Config.IsShowingGroup;
            IsShowingExtraDetail = GlobalDataHelper<AppConfig>.Config.IsShowingExtraDetail;
        }

        public DelegateCommand CheckUpdateCommand =>
            _CheckUpdateCommand ?? (_CheckUpdateCommand = new DelegateCommand(OnCheckUpdate));

        public DelegateCommand<SelectionChangedEventArgs> PaneDisplayModeChangedCommand =>
            _PaneDisplayModeChangedCommand ?? (_PaneDisplayModeChangedCommand =
                new DelegateCommand<SelectionChangedEventArgs>(OnPaneDisplayModeChanged));

        public DelegateCommand<SelectionChangedEventArgs> InstallModeChangedCommand =>
            _InstallModeChangedCommand ?? (_InstallModeChangedCommand =
                new DelegateCommand<SelectionChangedEventArgs>(OnInstallModeChanged));

        public DelegateCommand<SelectionChangedEventArgs> IdentifyPackageModeChangedCommand =>
            _IdentifyPackageModeChangedCommand ?? (_IdentifyPackageModeChangedCommand =
                new DelegateCommand<SelectionChangedEventArgs>(OnIdentifyPackageModeChanged));

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public int PaneIndex
        {
            get => _PaneIndex;
            set => SetProperty(ref _PaneIndex, value);
        }

        public int InstallModeIndex
        {
            get => _InstallModeIndex;
            set => SetProperty(ref _InstallModeIndex, value);
        }

        public int IdentifyPackageModeIndex
        {
            get => _IdentifyPackageModeIndex;
            set => SetProperty(ref _IdentifyPackageModeIndex, value);
        }

        public bool IsIDMEnabled
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

        public bool IsVisibleIDM
        {
            get => _IsVisibleIDM;
            set => SetProperty(ref _IsVisibleIDM, value);
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

        private void OnPaneDisplayModeChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            if (e.AddedItems[0] is NavigationViewPaneDisplayMode item)
                if (!item.Equals(GlobalDataHelper<AppConfig>.Config.PaneDisplayMode))
                {
                    GlobalDataHelper<AppConfig>.Config.PaneDisplayMode = item;
                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                    MainWindowViewModel.Instance.PaneDisplayMode = item;
                }
        }

        private void OnInstallModeChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            if (e.AddedItems[0] is InstallMode item)
                if (!item.Equals(GlobalDataHelper<AppConfig>.Config.InstallMode))
                {
                    GlobalDataHelper<AppConfig>.Config.InstallMode = item;

                    if (item.Equals(InstallMode.Internal))
                    {
                        IsVisibleIDM = true;
                    }
                    else
                    {
                        if (!IsOsSupported())
                        {
                            MessageBox.Error(
                                "Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later",
                                "OS is not Supported");
                            InstallModeIndex = 1;
                        }
                        else
                        {
                            IsVisibleIDM = false;
                        }
                    }

                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                }
        }

        private void OnIdentifyPackageModeChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            if (e.AddedItems[0] is IdentifyPackageMode item)
                if (!item.Equals(GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode))
                {
                    GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode = item;

                    if (item.Equals(IdentifyPackageMode.Wingetcli))
                        if (!IsOsSupported())
                        {
                            MessageBox.Error(
                                "Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later",
                                "OS is not Supported");
                            IdentifyPackageModeIndex = 0;
                        }

                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                }
        }

        private void OnCheckUpdate()
        {
            try
            {
                var ver =
                    UpdateHelper.CheckForUpdateGithubRelease("HandyOrg", "HandyWinGet");

                if (ver.IsExistNewVersion)
                    Growl.AskGlobal("we found a new Version, do you want to download?", b =>
                    {
                        if (!b) return true;

                        var exeLocation = Environment.CurrentDirectory + @"\HandyWinGet.exe";

                        Process.Start(
                            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) +
                            @"\HandyUpdater.exe",
                            $"{Version} {ver.TagName.Replace("v", "")} {Environment.CurrentDirectory} {exeLocation} {ver.Asset[0].browser_download_url} ");
                        Environment.Exit(0);
                        return true;
                    });
                else
                    Growl.InfoGlobal("you are using Latest Version.");
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        public bool IsOsSupported()
        {
            var subKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion";
            var key = Registry.LocalMachine;
            var skey = key.OpenSubKey(subKey);

            var name = skey?.GetValue("ProductName")?.ToString();
            if (name != null && name.Contains("Windows 10"))
            {
                var releaseId =
                    Convert.ToInt32(Registry.GetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", ""));
                return releaseId >= 1709;
            }

            return false;
        }
    }
}