using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Controls;

namespace HandyWinget_GUI.ViewModels
{
    public class CreatePackagesViewModel : BindableBase
    {
        public DelegateCommand<SelectionChangedEventArgs> SwitchItemCmd { get; private set; }
        public DelegateCommand<object> CreateCommand { get; set; }
        public DelegateCommand GetHashCommand { get; set; }

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
        public CreatePackagesViewModel()
        {
            GetHashCommand = new DelegateCommand(OnHash);
            CreateCommand = new DelegateCommand<object>(OnCreate);
            SwitchItemCmd = new DelegateCommand<SelectionChangedEventArgs>(Switch);
        }

        private void OnHash()
        {
            IsEnabled = false;
            OnDownloadClick();
        }

        private readonly SHA256 Sha256 = SHA256.Create();

        private byte[] GetHashSha256(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                return Sha256.ComputeHash(stream);
            }
        }
        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes)
            {
                result += b.ToString("x2");
            }

            return result;
        }
        private void OnCreate(object e)
        {
            string path = $"{Version}.yaml";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            string tags = string.Empty;
            if (e is UIElementCollection data)
            {
                foreach (object item in data)
                {
                    Tag correctItem = item as Tag;
                    if (correctItem != null && !correctItem.Content.ToString().Equals(LocalizationManager.Instance.Localize("DefaultTag")))
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
            File.AppendAllText($"{Version}.yaml", $"Id: {PackageId}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Version: {Version}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Name: {AppName}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Publisher: {Publisher}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"License: {License}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"LicenseUrl: {LicenseUrl}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"AppMoniker: {AppMoniker}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Tags: {tags}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Description: {Description}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Homepage: {HomePage}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"Installers:" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"  - Arch: {Arch}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"    Url: {URL}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"    Sha256: {Hash}" + Environment.NewLine);
            File.AppendAllText($"{Version}.yaml", $"    InstallerType: {ext}");
            if (ext != null && ext.ToLower().Equals("exe"))
            {
                File.AppendAllText($"{Version}.yaml", Environment.NewLine);
                File.AppendAllText($"{Version}.yaml", "    Switches:" + Environment.NewLine);
                File.AppendAllText($"{Version}.yaml", "      Silent: /S" + Environment.NewLine);
                File.AppendAllText($"{Version}.yaml", "      SilentWithProgress: /S" + Environment.NewLine);
            }
        }

        private string Arch = string.Empty;
        private void Switch(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            if (e.AddedItems[0] is ComboBoxItem item)
            {
                Arch = item.Content.ToString();
            }
        }

        #region Downloader
        private readonly string location = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\";
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
            Hash = BytesToString(GetHashSha256(location + Path.GetFileName(URL)));
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
