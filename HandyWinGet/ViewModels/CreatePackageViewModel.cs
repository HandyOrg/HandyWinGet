using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyWinGet.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using YamlDotNet.Serialization;

namespace HandyWinGet.ViewModels
{
    public class CreatePackageViewModel : BindableBase
    {
        public enum GenerateMode
        {
            CopyToClipboard,
            SaveToFile
        }

        public void GenerateScript(GenerateMode mode)
        {
            if (!string.IsNullOrEmpty(AppName) && !string.IsNullOrEmpty(Publisher)
                                               && !string.IsNullOrEmpty(PackageId) && !string.IsNullOrEmpty(Version)
                                               && !string.IsNullOrEmpty(License) && !string.IsNullOrEmpty(URL) &&
                                               URL.IsUrl())
            {
                var tags = string.Join(",", TagDataList.Select(p => p.Content));

                var ext = Path.GetExtension(URL)?.Replace(".", "").Trim();
                if (ext != null && ext.ToLower().Equals("msixbundle")) ext = "Msix";

                var builder = new YamlModel
                {
                    Id = PackageId,
                    Version = Version,
                    Name = AppName,
                    Publisher = Publisher,
                    License = License,
                    LicenseUrl = LicenseUrl,
                    AppMoniker = AppMoniker,
                    Tags = tags,
                    Description = Description,
                    Homepage = HomePage,
                    Installers = new List<Installer>
                    {
                        new()
                        {
                            Arch = SelectedArchitecture?.Content.ToString(),
                            Url = URL,
                            Sha256 = Hash
                        }
                    },
                    InstallerType = ext,
                    Switches = ext != null && ext.ToLower().Equals("exe")
                        ? new Switches
                        {
                            Silent = "/S",
                            SilentWithProgress = "/S"
                        }
                        : new Switches()
                };

                var serializer = new SerializerBuilder().Build();
                var yaml = serializer.Serialize(builder);
                switch (mode)
                {
                    case GenerateMode.CopyToClipboard:
                        Clipboard.SetText(yaml);
                        Growl.SuccessGlobal("Script Copied to clipboard.");
                        ClearInputs();
                        break;
                    case GenerateMode.SaveToFile:
                        var dialog = new SaveFileDialog();
                        dialog.Title = "Save Package";
                        dialog.FileName = $"{Version}.yaml";
                        dialog.DefaultExt = "yaml";
                        dialog.Filter = "Yaml File (*.yaml)|*.yaml";
                        if (dialog.ShowDialog() == true)
                        {
                            File.WriteAllText(dialog.FileName, yaml);
                            ClearInputs();
                        }

                        break;
                }
            }
            else
            {
                Growl.ErrorGlobal("Required fields must be filled");
            }
        }

        private void CreatePackage()
        {
            GenerateScript(GenerateMode.SaveToFile);
        }

        private void CopyToClipboard()
        {
            GenerateScript(GenerateMode.CopyToClipboard);
        }

        private void GetHash()
        {
            if (!string.IsNullOrEmpty(URL) && URL.IsUrl())
            {
                IsEnabled = false;
                OnDownloadClick();
            }
            else
            {
                Growl.ErrorGlobal("Url field is Empty or Invalid");
            }
        }

        private void AddTag()
        {
            if (string.IsNullOrEmpty(TagName))
            {
                Growl.Warning("Please Enter Content");
                return;
            }

            TagDataList.Add(new Tag
            {
                Content = TagName,
                ShowCloseButton = true
            });
            TagName = string.Empty;
        }

        public void ClearInputs()
        {
            AppName = string.Empty;
            Publisher = string.Empty;
            PackageId = string.Empty;
            Version = string.Empty;
            AppMoniker = string.Empty;
            TagName = string.Empty;
            Description = string.Empty;
            HomePage = string.Empty;
            License = string.Empty;
            LicenseUrl = string.Empty;
            URL = string.Empty;
            Hash = string.Empty;
            Progress = 0;
            TagDataList.Clear();
        }

        #region Commands

        private DelegateCommand _GetHashCmd;

        public DelegateCommand GetHashCmd =>
            _GetHashCmd ?? (_GetHashCmd = new DelegateCommand(GetHash));

        private DelegateCommand _CreatePackageCmd;

        public DelegateCommand CreatePackageCmd =>
            _CreatePackageCmd ?? (_CreatePackageCmd = new DelegateCommand(CreatePackage));

        private DelegateCommand _CopyToClipboardCmd;

        public DelegateCommand CopyToClipboardCmd =>
            _CopyToClipboardCmd ?? (_CopyToClipboardCmd = new DelegateCommand(CopyToClipboard));

        private DelegateCommand _AddTagCmd;

        public DelegateCommand AddTagCmd =>
            _AddTagCmd ?? (_AddTagCmd = new DelegateCommand(AddTag));

        #endregion

        #region Property

        private string _tagName;

        public string TagName
        {
            get => _tagName;
            set => SetProperty(ref _tagName, value);
        }

        private bool _IsEnabled = true;

        public bool IsEnabled
        {
            get => _IsEnabled;
            set => SetProperty(ref _IsEnabled, value);
        }

        private int _Progress;

        public int Progress
        {
            get => _Progress;
            set => SetProperty(ref _Progress, value);
        }

        private string _AppName;

        public string AppName
        {
            get => _AppName;
            set => SetProperty(ref _AppName, value);
        }

        private string _Publisher;

        public string Publisher
        {
            get => _Publisher;
            set => SetProperty(ref _Publisher, value);
        }

        private string _PackageId;

        public string PackageId
        {
            get => _PackageId;
            set => SetProperty(ref _PackageId, value);
        }

        private string _Version;

        public string Version
        {
            get => _Version;
            set => SetProperty(ref _Version, value);
        }

        private string _AppMoniker;

        public string AppMoniker
        {
            get => _AppMoniker;
            set => SetProperty(ref _AppMoniker, value);
        }

        private string _Description;

        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private string _HomePage;

        public string HomePage
        {
            get => _HomePage;
            set => SetProperty(ref _HomePage, value);
        }

        private string _License;

        public string License
        {
            get => _License;
            set => SetProperty(ref _License, value);
        }

        private string _LicenseUrl;

        public string LicenseUrl
        {
            get => _LicenseUrl;
            set => SetProperty(ref _LicenseUrl, value);
        }

        private string _URL;

        public string URL
        {
            get => _URL;
            set => SetProperty(ref _URL, value);
        }

        private string _Hash;

        public string Hash
        {
            get => _Hash;
            set => SetProperty(ref _Hash, value);
        }

        private ComboBoxItem _SelectedArchitecture;

        public ComboBoxItem SelectedArchitecture
        {
            get => _SelectedArchitecture;
            set => SetProperty(ref _SelectedArchitecture, value);
        }

        private ObservableCollection<Tag> _TagDataList = new();

        public ObservableCollection<Tag> TagDataList
        {
            get => _TagDataList;
            set => SetProperty(ref _TagDataList, value);
        }

        #endregion

        #region Downloader

        private readonly string location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\";

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Progress = 0;
            Hash = CryptographyHelper.GenerateSHA256ForFile(location + Path.GetFileName(URL));
            IsEnabled = true;
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var bytesIn = double.Parse(e.BytesReceived.ToString());
            var totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            var percentage = bytesIn / totalBytes * 100;
            Progress = int.Parse(Math.Truncate(percentage).ToString());
        }

        private async void OnDownloadClick()
        {
            try
            {
                Progress = 0;
                var downloader = new DownloadService();
                downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                await downloader.DownloadFileAsync(URL, location + Path.GetFileName(URL));
            }
            catch (NotSupportedException)
            {
            }
            catch (ArgumentException)
            {
            }
        }

        #endregion
    }
}