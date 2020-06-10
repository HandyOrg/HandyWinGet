using HandyControl.Controls;
using HandyWinget_GUI.Assets.Languages;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace HandyWinget_GUI.ViewModels
{
    public class UpdaterViewModel : BindableBase
    {
        #region Property
        private Visibility _IsUpdateExist = Visibility.Collapsed;
        public Visibility IsUpdateExist
        {
            get => _IsUpdateExist;
            set => SetProperty(ref _IsUpdateExist, value);
        }

        private string _version;
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private string _createdAt;
        public string CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        private string _publishedAt;
        public string PublishedAt
        {
            get => _publishedAt;
            set => SetProperty(ref _publishedAt, value);
        }

        private string _downloadUrl;
        public string DownloadUrl
        {
            get => _downloadUrl;
            set => SetProperty(ref _downloadUrl, value);
        }

        private string _currentVersion;
        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        private string _changeLog;
        public string ChangeLog
        {
            get => _changeLog;
            set => SetProperty(ref _changeLog, value);
        }
        #endregion

        public DelegateCommand CheckUpdateCommand { get; private set; }
        public DelegateCommand DownloadCommand { get; private set; }

        public UpdaterViewModel()
        {
            CheckUpdateCommand = new DelegateCommand(CheckforUpdate);
            DownloadCommand = new DelegateCommand(OnDownloadClick);
        }

        private void OnDownloadClick()
        {
            string exeLocation = Environment.CurrentDirectory + @"\HandyWinGet_GUI.exe";

            Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\HandyUpdater.exe", $"{CurrentVersion} {Version} {Environment.CurrentDirectory} {exeLocation} {DownloadUrl} ");
            Environment.Exit(0);
        }

        private void CheckforUpdate()
        {
            try
            {
                UpdateHelper.GithubReleaseModel ver = UpdateHelper.CheckForUpdateGithubRelease("HandyOrg", "HandyWinGet-GUI");
                CreatedAt = ver.CreatedAt.ToString();
                PublishedAt = ver.PublishedAt.ToString();
                DownloadUrl = ver.Asset[0].browser_download_url;
                CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Version = ver.TagName.Replace("v", "");
                ChangeLog = ver.Changelog;
                if (ver.IsExistNewVersion)
                {
                    IsUpdateExist = Visibility.Visible;
                    Growl.Success(Lang.ResourceManager.GetString("NewVersionFound"));
                }
                else
                {
                    IsUpdateExist = Visibility.Collapsed;
                    Growl.Info(Lang.ResourceManager.GetString("LatestVersion"));
                }
            }
            catch (System.Exception)
            {
                IsUpdateExist = Visibility.Collapsed;
                Growl.Error(Lang.ResourceManager.GetString("NoNewVersion"));
            }
        }
    }
}
