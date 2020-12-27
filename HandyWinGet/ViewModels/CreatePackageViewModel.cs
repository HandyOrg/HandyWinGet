using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyWinGet.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Serialization;

namespace HandyWinGet.ViewModels
{
    public class CreatePackageViewModel : BindableBase
    {
        #region Commands

        private DelegateCommand _getHashCmd;
        private DelegateCommand _createPackageCmd;
        private DelegateCommand _copyToClipboardCmd;
        private DelegateCommand _addTagCmd;

        public DelegateCommand GetHashCmd =>
            _getHashCmd ??= new DelegateCommand(GetHash);
        public DelegateCommand CreatePackageCmd =>
            _createPackageCmd ??= new DelegateCommand(CreatePackage);

        public DelegateCommand CopyToClipboardCmd =>
            _copyToClipboardCmd ??= new DelegateCommand(CopyToClipboard);

        public DelegateCommand AddTagCmd =>
            _addTagCmd ??= new DelegateCommand(AddTag);

        #endregion

        #region Property

        private ComboBoxItem _selectedArchitecture;
        private ObservableCollection<Tag> _tagDataList = new();
        private string _tagName;
        private bool _isEnabled = true;
        private int _progress;
        private string _appName;
        private string _publisher;
        private string _packageId;
        private string _version;
        private string _appMoniker;
        private string _description;
        private string _homePage;
        private string _license;
        private string _licenseUrl;
        private string _url;
        private string _hash;

        public string TagName
        {
            get => _tagName;
            set => SetProperty(ref _tagName, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        public string Publisher
        {
            get => _publisher;
            set => SetProperty(ref _publisher, value);
        }

        public string PackageId
        {
            get => _packageId;
            set => SetProperty(ref _packageId, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string AppMoniker
        {
            get => _appMoniker;
            set => SetProperty(ref _appMoniker, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string HomePage
        {
            get => _homePage;
            set => SetProperty(ref _homePage, value);
        }

        public string License
        {
            get => _license;
            set => SetProperty(ref _license, value);
        }

        public string LicenseUrl
        {
            get => _licenseUrl;
            set => SetProperty(ref _licenseUrl, value);
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public string Hash
        {
            get => _hash;
            set => SetProperty(ref _hash, value);
        }

        public ComboBoxItem SelectedArchitecture
        {
            get => _selectedArchitecture;
            set => SetProperty(ref _selectedArchitecture, value);
        }

        public ObservableCollection<Tag> TagDataList
        {
            get => _tagDataList;
            set => SetProperty(ref _tagDataList, value);
        }

        #endregion

        public enum GenerateMode
        {
            CopyToClipboard,
            SaveToFile
        }

        public void GenerateScript(GenerateMode mode)
        {
            if (!string.IsNullOrEmpty(AppName) && !string.IsNullOrEmpty(Publisher)
                                               && !string.IsNullOrEmpty(PackageId) && !string.IsNullOrEmpty(Version)
                                               && !string.IsNullOrEmpty(License) && !string.IsNullOrEmpty(Url) &&
                                               Url.IsUrl())
            {
                var tags = string.Join(",", TagDataList.Select(p => p.Content));

                var ext = Path.GetExtension(Url)?.Replace(".", "").Trim();
                if (ext != null && ext.ToLower().Equals("msixbundle"))
                {
                    ext = "Msix";
                }

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
                            Url = Url,
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
            if (!string.IsNullOrEmpty(Url) && Url.IsUrl())
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
            Url = string.Empty;
            Hash = string.Empty;
            Progress = 0;
            TagDataList.Clear();
        }

        #region Downloader

        private readonly string _location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\";

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Progress = 0;
            Hash = CryptographyHelper.GenerateSHA256ForFile(_location + Path.GetFileName(Url));
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
                await downloader.DownloadFileAsync(Url, _location + Path.GetFileName(Url));
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