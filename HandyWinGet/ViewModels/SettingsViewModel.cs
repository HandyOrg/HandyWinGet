using HandyControl.Controls;
using HandyWinGet.Data;
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
            _CheckUpdateCommand ?? (_CheckUpdateCommand = new DelegateCommand(CheckUpdate));

        private DelegateCommand<SelectionChangedEventArgs> _PaneDisplayModeChangedCommand;
        public DelegateCommand<SelectionChangedEventArgs> PaneDisplayModeChangedCommand =>
            _PaneDisplayModeChangedCommand ?? (_PaneDisplayModeChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(PaneDisplayModeChanged));

        private string _version;
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private int _PaneIndex;
        public int PaneIndex
        {
            get { return _PaneIndex; }
            set { SetProperty(ref _PaneIndex, value); }
        }

        public SettingsViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            PaneIndex = (int)GlobalDataHelper<AppConfig>.Config.PaneDisplayMode;
        }

        void PaneDisplayModeChanged(SelectionChangedEventArgs e)
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

        void CheckUpdate()
        {
            try
            {
                UpdateHelper.GithubReleaseModel ver = UpdateHelper.CheckForUpdateGithubRelease("HandyOrg", "HandyWinGet-GUI");

                if (ver.IsExistNewVersion)
                {
                    Growl.AskGlobal("we found a new Version, do you want to download?", b =>
                    {
                        if (!b)
                        {
                            return true;
                        }

                        string exeLocation = Environment.CurrentDirectory + @"\HandyWinGet_GUI.exe";

                        Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\HandyUpdater.exe", $"{Version} {ver.TagName.Replace("v", "")} {Environment.CurrentDirectory} {exeLocation} {ver.Asset[0].browser_download_url} ");
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
    }
}
