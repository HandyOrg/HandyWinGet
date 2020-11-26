using HandyControl.Controls;
using HandyControl.Tools;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
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

        private DelegateCommand<object> _CreatePackageCmd;
        public DelegateCommand<object> CreatePackageCmd =>
            _CreatePackageCmd ?? (_CreatePackageCmd = new DelegateCommand<object>(CreatePackage));

        private DelegateCommand<object> _CopyToClipboardCmd;
        public DelegateCommand<object> CopyToClipboardCmd =>
            _CopyToClipboardCmd ?? (_CopyToClipboardCmd = new DelegateCommand<object>(CopyToClipboard));

        #endregion

        #region Property
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
            get { return _SelectedArchitecture; }
            set { SetProperty(ref _SelectedArchitecture, value); }
        }

        #endregion
        public CreatePackageViewModel()
        {

        }

        public void GenerateScript(GenerateMode mode, object param)
        {
            if (!string.IsNullOrEmpty(AppName) && !string.IsNullOrEmpty(Publisher)
                && !string.IsNullOrEmpty(PackageId) && !string.IsNullOrEmpty(Version)
                && !string.IsNullOrEmpty(License) && !string.IsNullOrEmpty(URL) && URL.IsUrl())
            {
                string tags = string.Empty;
                if (param is UIElementCollection data)
                {
                    foreach (object item in data)
                    {
                        Tag correctItem = item as Tag;
                        if (correctItem != null && !correctItem.Content.ToString().Equals("Tags (Keywords):"))
                        {
                            tags += correctItem.Content.ToString().Trim() + ", ";
                        }
                    }
                }
                if (tags != null && tags.Trim().Count() > 0)
                {
                    tags = tags.Remove(tags.TrimEnd().Count() - 1, 1);
                }

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
                builder.AppendLine($"  - Arch: {SelectedArchitecture.Content}");
                builder.AppendLine($"    Url: {URL}");
                builder.AppendLine($"    Sha256: {Hash}");
                builder.AppendLine($"    InstallerType: {ext}");
                if (ext != null && ext.ToLower().Equals("exe"))
                {
                    builder.AppendLine(Environment.NewLine);
                    builder.AppendLine("    Switches:");
                    builder.AppendLine("      Silent: /S");
                    builder.AppendLine("      SilentWithProgress: /S");
                }

                switch (mode)
                {
                    case GenerateMode.CopyToClipboard:
                        Clipboard.SetText(builder.ToString());
                        Growl.SuccessGlobal("Script Copied to clipboard.");
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
                        }
                        break;
                }
            }
            else
            {
                Growl.ErrorGlobal("Required fields must be filled");
            }
        }

        void CreatePackage(object param)
        {
            GenerateScript(GenerateMode.SaveToFile, param);
        }

        void CopyToClipboard(object param)
        {
            GenerateScript(GenerateMode.CopyToClipboard, param);
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

        #region Downloader
        private readonly string location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\";
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            Progress = int.Parse(Math.Truncate(percentage).ToString());
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            Progress = 0;
            Hash = CryptographyHelper.GenerateSHA256ForFile(location + Path.GetFileName(URL));
            IsEnabled = true;
        }

        private readonly WebClient client = new WebClient();
        private void OnDownloadClick()
        {
            try
            {
                Progress = 0;
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                client.DownloadFileAsync(new Uri(URL), location + Path.GetFileName(URL));
            }
            catch (NotSupportedException) { }
            catch (ArgumentException) { }
        }
        #endregion
    }
}
