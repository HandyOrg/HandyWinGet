using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HandyWinGet.ViewModels
{
    public class CreatePackageViewModel : BindableBase
    {
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

        private ObservableCollection<Tag> _TagDataList = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> TagDataList
        {
            get => _TagDataList;
            set => SetProperty(ref _TagDataList, value);
        }
        #endregion
        public CreatePackageViewModel()
        {

        }

        public void GenerateScript(GenerateMode mode)
        {
            if (!string.IsNullOrEmpty(AppName) && !string.IsNullOrEmpty(Publisher)
                && !string.IsNullOrEmpty(PackageId) && !string.IsNullOrEmpty(Version)
                && !string.IsNullOrEmpty(License) && !string.IsNullOrEmpty(URL) && URL.IsUrl())
            {

                var tags = string.Join(",", TagDataList.Select(p => p.Content));

                string ext = Path.GetExtension(URL)?.Replace(".", "").Trim();
                if (ext != null && ext.ToLower().Equals("msixbundle"))
                {
                    ext = "Msix";
                }
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"Id: {PackageId}");
                builder.AppendLine($"Version: {Version}");
                builder.AppendLine($"Name: {AppName}");
                builder.AppendLine($"Publisher: {Publisher}");
                builder.AppendLine($"License: {License}");
                builder.AppendLine($"LicenseUrl: {LicenseUrl}");
                builder.AppendLine($"AppMoniker: {AppMoniker}");
                builder.AppendLine($"Tags: {tags}");
                builder.AppendLine($"Description: {Description}");
                builder.AppendLine($"Homepage: {HomePage}");
                builder.AppendLine($"Installers:");
                builder.AppendLine($"  - Arch: {SelectedArchitecture?.Content}");
                builder.AppendLine($"    Url: {URL}");
                builder.AppendLine($"    Sha256: {Hash}");
                builder.AppendLine($"    InstallerType: {ext}");
                if (ext != null && ext.ToLower().Equals("exe"))
                {
                    builder.AppendLine();
                    builder.AppendLine("    Switches:");
                    builder.AppendLine("      Silent: /S");
                    builder.AppendLine("      SilentWithProgress: /S");
                }

                switch (mode)
                {
                    case GenerateMode.CopyToClipboard:
                        Clipboard.SetText(builder.ToString());
                        Growl.SuccessGlobal("Script Copied to clipboard.");
                        ClearInputs();
                        break;
                    case GenerateMode.SaveToFile:
                        SaveFileDialog dialog = new SaveFileDialog();
                        dialog.Title = "Save Package";
                        dialog.FileName = $"{Version}.yaml";
                        dialog.DefaultExt = "yaml";
                        dialog.Filter = "Yaml File (*.yaml)|*.yaml";
                        if (dialog.ShowDialog() == true)
                        {
                            File.WriteAllText(dialog.FileName, builder.ToString());
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

        void CreatePackage()
        {
            GenerateScript(GenerateMode.SaveToFile);
        }

        void CopyToClipboard()
        {
            GenerateScript(GenerateMode.CopyToClipboard);
        }

        public enum GenerateMode
        {
            CopyToClipboard,
            SaveToFile
        }
        void GetHash()
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

        void AddTag()
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
            SelectedArchitecture.Content = string.Empty;
            URL = string.Empty;
            Hash = string.Empty;
            Progress = 0;
            TagDataList.Clear();
        }

        #region Downloader
        private readonly string location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\";

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Progress = 0;
            Hash = CryptographyHelper.GenerateSHA256ForFile(location + Path.GetFileName(URL));
            IsEnabled = true;
        }

        private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
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
            catch (NotSupportedException) { }
            catch (ArgumentException) { }
        }
        #endregion
    }
}
