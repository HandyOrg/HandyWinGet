using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Views;
using Microsoft.Win32;
using ModernWpf.Controls;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;

namespace HandyWinGet.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private DelegateCommand _CheckUpdateCommand;
        public DelegateCommand CheckUpdateCommand =>
            _CheckUpdateCommand ?? (_CheckUpdateCommand = new DelegateCommand(OnCheckUpdate));

        private DelegateCommand<SelectionChangedEventArgs> _PaneDisplayModeChangedCommand;
        public DelegateCommand<SelectionChangedEventArgs> PaneDisplayModeChangedCommand =>
            _PaneDisplayModeChangedCommand ?? (_PaneDisplayModeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(OnPaneDisplayModeChanged));

        private DelegateCommand<SelectionChangedEventArgs> _InstallModeChangedCommand;
        public DelegateCommand<SelectionChangedEventArgs> InstallModeChangedCommand =>
            _InstallModeChangedCommand ?? (_InstallModeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(OnInstallModeChanged));

        private DelegateCommand<SelectionChangedEventArgs> _IdentifyPackageModeChangedCommand;
        public DelegateCommand<SelectionChangedEventArgs> IdentifyPackageModeChangedCommand =>
            _IdentifyPackageModeChangedCommand ?? (_IdentifyPackageModeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(OnIdentifyPackageModeChanged));

        private string _version;
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private int _PaneIndex;
        public int PaneIndex
        {
            get => _PaneIndex;
            set => SetProperty(ref _PaneIndex, value);
        }

        private int _InstallModeIndex;
        public int InstallModeIndex
        {
            get => _InstallModeIndex;
            set => SetProperty(ref _InstallModeIndex, value);
        }

        private int _IdentifyPackageModeIndex;
        public int IdentifyPackageModeIndex
        {
            get => _IdentifyPackageModeIndex;
            set => SetProperty(ref _IdentifyPackageModeIndex, value);
        }

        private bool _isIdmEnabled;
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

        private bool _IsVisibleIDM;
        public bool IsVisibleIDM
        {
            get => _IsVisibleIDM;
            set => SetProperty(ref _IsVisibleIDM, value);
        }

        private bool _isShowingGroup;
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

        private bool _isShowingExtraDetail;
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

        public SettingsViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            PaneIndex = (int)GlobalDataHelper<AppConfig>.Config.PaneDisplayMode;
            InstallModeIndex = (int)GlobalDataHelper<AppConfig>.Config.InstallMode;
            IsVisibleIDM = InstallModeIndex == (int)InstallMode.Internal;
            IdentifyPackageModeIndex = (int)GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode;

            IsIDMEnabled = GlobalDataHelper<AppConfig>.Config.IsIDMEnabled;
            IsShowingGroup = GlobalDataHelper<AppConfig>.Config.IsShowingGroup;
            IsShowingExtraDetail = GlobalDataHelper<AppConfig>.Config.IsShowingExtraDetail;
        }

        void OnPaneDisplayModeChanged(SelectionChangedEventArgs e)
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

        void OnInstallModeChanged(SelectionChangedEventArgs e)
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
                        IsVisibleIDM = true;
                    }
                    else
                    {
                        if (!IsOsSupported())
                        {
                            HandyControl.Controls.MessageBox.Error("Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later", "OS is not Supported");
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
        }

        void OnIdentifyPackageModeChanged(SelectionChangedEventArgs e)
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
                        if (!IsOsSupported())
                        {
                            HandyControl.Controls.MessageBox.Error("Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later", "OS is not Supported");
                            IdentifyPackageModeIndex = 0;
                        }
                    }

                    GlobalDataHelper<AppConfig>.Save();
                    GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
                }
            }
        }
        void OnCheckUpdate()
        {
            try
            {
                UpdateHelper.GithubReleaseModel ver = UpdateHelper.CheckForUpdateGithubRelease("HandyOrg", "HandyWinGet");

                if (ver.IsExistNewVersion)
                {
                    Growl.AskGlobal("we found a new Version, do you want to download?", b =>
                    {
                        if (!b)
                        {
                            return true;
                        }

                        string exeLocation = Environment.CurrentDirectory + @"\HandyWinGet.exe";

                        Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + @"\HandyUpdater.exe", $"{Version} {ver.TagName.Replace("v", "")} {Environment.CurrentDirectory} {exeLocation} {ver.Asset[0].browser_download_url} ");
                        Environment.Exit(0);
                        return true;
                    });
                }
                else
                {
                    Growl.InfoGlobal("you are using Latest Version.");
                }
            }
            catch (System.Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        public bool IsOsSupported()
        {
            string subKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion";
            RegistryKey key = Registry.LocalMachine;
            RegistryKey skey = key.OpenSubKey(subKey);

            string name = skey?.GetValue("ProductName")?.ToString();
            if (name != null && name.Contains("Windows 10"))
            {
                int releaseId = Convert.ToInt32(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", ""));
                return releaseId >= 1709;
            }
            else
            {
                return false;
            }
        }

    }
}
