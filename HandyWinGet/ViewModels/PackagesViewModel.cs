using Downloader;
using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Models;
using LibGit2Sharp;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YamlDotNet.Serialization;
using MessageBox = HandyControl.Controls.MessageBox;

namespace HandyWinGet.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        internal static PackagesViewModel Instance;
        private static readonly object _lock = new object();

        public string _Id = string.Empty;
        private VersionModel SelectedPackage = new VersionModel();

        List<InstalledAppModel> InstalledApps = new List<InstalledAppModel>();
        readonly List<string> keys = new List<string>() {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(DataList);
        public ICollectionView ComboView => CollectionViewSource.GetDefaultView(DataListVersion);
        private readonly string path = Assembly.GetExecutingAssembly().Location.Replace(Path.GetFileName(Assembly.GetExecutingAssembly().Location), "") + @"pkgs";

        #region Command
        private DelegateCommand<string> _ButtonCmd;
        public DelegateCommand<string> ButtonCmd =>
            _ButtonCmd ?? (_ButtonCmd = new DelegateCommand<string>(OnButtonAction));

        private DelegateCommand<SelectionChangedEventArgs> _ItemChangedCmd;
        public DelegateCommand<SelectionChangedEventArgs> ItemChangedCmd =>
            _ItemChangedCmd ?? (_ItemChangedCmd = new DelegateCommand<SelectionChangedEventArgs>(ItemChanged));

        #endregion

        #region Property
        private ObservableCollection<PackageModel> _DataList;
        public ObservableCollection<PackageModel> DataList
        {
            get => _DataList;
            set => SetProperty(ref _DataList, value);
        }

        private ObservableCollection<VersionModel> _DataListVersion;
        public ObservableCollection<VersionModel> DataListVersion
        {
            get => _DataListVersion;
            set => SetProperty(ref _DataListVersion, value);
        }

        private bool _DataGot;
        public bool DataGot
        {
            get => _DataGot;
            set => SetProperty(ref _DataGot, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ItemsView.Refresh();
            }
        }

        private string _LoadingStatus;
        public string LoadingStatus
        {
            get => _LoadingStatus;
            set => SetProperty(ref _LoadingStatus, value);
        }

        private string _UpdatedDate;
        public string UpdatedDate
        {
            get => _UpdatedDate;
            set => SetProperty(ref _UpdatedDate, value);
        }

        private int _Progress = 0;
        public int Progress
        {
            get => _Progress;
            set => SetProperty(ref _Progress, value);
        }

        private bool _IsVisibleProgressButton;
        public bool IsVisibleProgressButton
        {
            get => _IsVisibleProgressButton;
            set => SetProperty(ref _IsVisibleProgressButton, value);
        }

        private bool _IsCheckedProgressButton = true;
        public bool IsCheckedProgressButton
        {
            get => _IsCheckedProgressButton;
            set
            {
                SetProperty(ref _IsCheckedProgressButton, value);
                if (!value)
                {
                    downloader.CancelAsync();
                }
            }
        }

        private DataGridRowDetailsVisibilityMode _RowDetailsVisibilityMode;
        public DataGridRowDetailsVisibilityMode RowDetailsVisibilityMode
        {
            get => GlobalDataHelper<AppConfig>.Config.IsExtraDetail
                    ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                    : DataGridRowDetailsVisibilityMode.Collapsed;
            set => SetProperty(ref _RowDetailsVisibilityMode, value);
        }

        #endregion

        public PackagesViewModel()
        {
            Instance = this;
            UpdatedDate = GlobalDataHelper<AppConfig>.Config.UpdatedDate.ToString();
            DataList = new ObservableCollection<PackageModel>();
            DataListVersion = new ObservableCollection<VersionModel>();
            BindingOperations.EnableCollectionSynchronization(DataList, _lock);
            BindingOperations.EnableCollectionSynchronization(DataListVersion, _lock);
            SetDataGridGrouping();
            ItemsView.Filter = new Predicate<object>(o => Filter(o as PackageModel));
            ComboView.Filter = new Predicate<object>(o => FilterCombo(o as VersionModel));
            GetPackages();
        }

        public void SetDataGridGrouping()
        {

            if (GlobalDataHelper<AppConfig>.Config.IsGroup)
            {
                ItemsView.GroupDescriptions.Add(new PropertyGroupDescription("Publisher"));
            }
            else
            {
                ItemsView.GroupDescriptions.Clear();
            }
        }

        #region Cloning Progress
        public bool RepositoryTransferProgress(TransferProgress progress)
        {
            LoadingStatus = $"Downloading {progress.ReceivedObjects} objects from {progress.TotalObjects}";
            return true;
        }

        private bool RepositoryOperationStartingProgress(RepositoryOperationContext context)
        {
            LoadingStatus = "Cloning Repository...";
            return true;
        }

        private void RepositoryOnCheckoutProgress(string path, int completedSteps, int totalSteps)
        {
            LoadingStatus = "Checkout Repository...";
        }

        #endregion

        public void CloneRepository()
        {
            DeleteDirectory(path);

            CloneOptions cloneOptions = new CloneOptions();
            cloneOptions.OnTransferProgress = RepositoryTransferProgress;
            cloneOptions.OnCheckoutProgress = RepositoryOnCheckoutProgress;
            cloneOptions.RepositoryOperationStarting = RepositoryOperationStartingProgress;

            Repository.Clone("https://github.com/microsoft/winget-pkgs.git", path, cloneOptions);
            UpdatedDate = DateTime.Now.ToString();
            GlobalDataHelper<AppConfig>.Config.UpdatedDate = DateTime.Now;
            GlobalDataHelper<AppConfig>.Save();
        }

        private void GetPackages(bool ForceUpdate = false)
        {
            Task.Run(() =>
            {
                if (!Directory.Exists(path + @"\manifests"))
                {
                    CloneRepository();
                }

                if (ForceUpdate)
                {
                    CloneRepository();
                }

                LoadingStatus = "Extracting packages...";

                var pkgs = GetAllDirectories(path + @"\manifests").OrderByDescending(x => x).ToList();
                FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), keys, InstalledApps);
                FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64), keys, InstalledApps);

                InstalledApps = InstalledApps.Distinct().ToList();
                foreach (string item in pkgs)
                {
                    var file = File.ReadAllText(item);
                    var input = new StringReader(file);

                    var deserializer = new DeserializerBuilder().Build();
                    var yamlObject = deserializer.Deserialize(input);
                    var serializer = new SerializerBuilder()
                        .JsonCompatible()
                        .Build();

                    var json = serializer.Serialize(yamlObject);
                    var yaml = System.Text.Json.JsonSerializer.Deserialize<YamlModel>(json);

                    string installedVersion = string.Empty;

                    bool isInstalled = false;
                    foreach (var itemApp in InstalledApps)
                    {
                        if (itemApp.DisplayName.Contains(yaml.Name))
                        {
                            installedVersion = $"Installed Version: {itemApp.Version}";
                            isInstalled = true;
                        }
                    }

                    var packge = new PackageModel { Publisher = yaml.Publisher, Name = yaml.Name, IsInstalled = isInstalled, 
                        Version = yaml.Version, Id = yaml.Id, Url = yaml.Installers[0].Url, Description = yaml.Description, LicenseUrl = yaml.LicenseUrl,
                        Homepage = yaml.Homepage, Arch = yaml.Id + " " + yaml.Installers[0].Arch, InstalledVersion = installedVersion };

                    if (!DataList.Contains(packge, new ItemEqualityComparer()))
                    {
                        DataList.Add(packge);
                    }
                    DataListVersion.Add(new VersionModel { Id = yaml.Id, Version = yaml.Version, Url = yaml.Installers[0].Url });
                }

                CleanRepo();
            }).ContinueWith(obj =>
            {
                DataGot = true;
            });
        }

        private bool Filter(PackageModel item)
        {
            return SearchText == null
                            || item.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1
                            || item.Publisher.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1;
        }

        private bool FilterCombo(VersionModel item)
        {
            return _Id == null || item.Id.Equals(_Id);
        }

        void ItemChanged(SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is DataGrid dg)
            {
                var dgItem = (PackageModel)dg.SelectedItem;

                if (dgItem != null)
                {
                    _Id = dgItem.Id;
                    ComboView.Refresh();
                    SelectedPackage = new VersionModel { Id = dgItem.Id, Version = dgItem.Version, Url = dgItem.Url };
                }
            }

            if (e.OriginalSource is HandyControl.Controls.ComboBox cmb)
            {
                var cmbItem = (VersionModel)cmb.SelectedItem;
                if (cmbItem != null)
                {
                    SelectedPackage = new VersionModel { Id = cmbItem.Id, Version = cmbItem.Version, Url = cmbItem.Url };
                }
            }
        }

        void OnButtonAction(string param)
        {
            switch (param)
            {
                case "Install":
                    switch (GlobalDataHelper<AppConfig>.Config.PackageInstallMode)
                    {
                        case PackageInstallMode.Wingetcli:
                            if (((App)System.Windows.Application.Current).IsWingetInstalled())
                            {
                                InstallWingetMode();
                            }
                            else
                            {
                                MessageBox.Error("Winget-cli is not installed, please download and install latest version.", "Install Winget");
                                StartProcess("https://github.com/microsoft/winget-cli/releases");
                            }

                            break;
                        case PackageInstallMode.Internal:
                            InstallInternalMode();
                            break;
                    }
                    break;
                case "Uninstall":
                    break;
                case "Refresh":
                    LoadingStatus = "Refreshing Packages...";
                    DataList.Clear();
                    DataListVersion.Clear();
                    InstalledApps.Clear();
                    DataGot = false;
                    GetPackages(true);
                    break;
            }
        }

        #region Clean Repo
        public IEnumerable<string> GetAllDirectories(string rootDirectory)
        {
            foreach (string directory in System.IO.Directory.GetDirectories(
                                                rootDirectory,
                                                "*",
                                                SearchOption.AllDirectories))
            {
                foreach (string file in System.IO.Directory.GetFiles(directory))
                {
                    yield return file;
                }
            }

        }

        private void CleanRepo()
        {
            DeleteDirectory(path + @"\DevOpsPipelineDefinitions");
            DeleteDirectory(path + @"\Tools");
            DeleteDirectory(path + @"\.git");
            DeleteDirectory(path + @"\.github");

            string[] filePaths = System.IO.Directory.GetFiles(path);
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }
        }

        public static void DeleteDirectory(string d)
        {
            if (System.IO.Directory.Exists(d))
            {
                foreach (string sub in System.IO.Directory.EnumerateDirectories(d))
                {
                    DeleteDirectory(sub);
                }
                foreach (string f in System.IO.Directory.EnumerateFiles(d))
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(f)
                    {
                        Attributes = FileAttributes.Normal
                    };
                    fi.Delete();
                }
                System.IO.Directory.Delete(d);
            }
        }
        #endregion

        public void InstallWingetMode()
        {
            if (SelectedPackage.Id != null)
            {
                DataGot = false;
                LoadingStatus = $"Preparing to download {SelectedPackage.Id}";

                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install {SelectedPackage.Id} {($"-v {SelectedPackage.Version}")}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };

                proc.OutputDataReceived += (o, args) =>
                {
                    string line = args.Data ?? "";

                    if (line.Contains("Download"))
                    {
                        LoadingStatus = "Downloading Package...";
                    }

                    if (line.Contains("hash"))
                    {
                        LoadingStatus = $"Validated hash for {SelectedPackage.Id}";
                    }

                    if (line.Contains("Installing"))
                    {
                        LoadingStatus = $"Installing {SelectedPackage.Id}";
                    }

                    if (line.Contains("Failed"))
                    {
                        LoadingStatus = $"Installation of {SelectedPackage.Id} failed";
                    }
                };

                proc.Exited += (o, args) =>
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        bool installFailed = (o as Process).ExitCode != 0;
                        LoadingStatus = installFailed
                            ? $"Installation of {SelectedPackage.Id} failed"
                            : $"Installed {SelectedPackage.Id}";

                        await Task.Delay(2000);
                        DataGot = true;
                    });
                };

                proc.Start();
                proc.BeginOutputReadLine();
            }
            else
            {
                Growl.InfoGlobal("Please select an application!");
            }
        }

        public async void InstallInternalMode()
        {
            try
            {
                if (SelectedPackage.Id != null)
                {
                    string url = RemoveComment(SelectedPackage.Url);

                    if (GlobalDataHelper<AppConfig>.Config.IsIDM)
                    {
                        DownloadWithIDM(url);
                    }
                    else
                    {
                        IsVisibleProgressButton = true;
                        LoadingStatus = $"Preparing to download {SelectedPackage.Id}";
                        DataGot = false;

                        _tempLocation = $"{location}{SelectedPackage.Id}-{SelectedPackage.Version}{GetExtension(url)}".Trim();
                        if (!File.Exists(_tempLocation))
                        {
                            downloader = new DownloadService();
                            downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                            downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                            await downloader.DownloadFileAsync(url, _tempLocation);
                        }
                        else
                        {
                            IsVisibleProgressButton = false;
                            DataGot = true;
                            StartProcess(_tempLocation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        public void StartProcess(string path)
        {
            try
            {
                ProcessStartInfo ps = new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Win32Exception ex)
            {
                if (!ex.Message.Contains("The system cannot find the file specified."))
                {
                    Growl.ErrorGlobal(ex.Message);

                }
            }
        }

        private void FindInstalledApps(RegistryKey regKey, List<string> keys, List<InstalledAppModel> installed)
        {
            foreach (string key in keys)
            {
                using (RegistryKey rk = regKey.OpenSubKey(key))
                {
                    if (rk == null)
                    {
                        continue;
                    }
                    foreach (string skName in rk.GetSubKeyNames())
                    {
                        using (RegistryKey sk = rk.OpenSubKey(skName))
                        {
                            if (sk.GetValue("DisplayName") != null)
                            {
                                try
                                {
                                    installed.Add(new InstalledAppModel
                                    {
                                        DisplayName = (string)sk.GetValue("DisplayName"),
                                        Version = (string)sk.GetValue("DisplayVersion"),
                                        Publisher = (string)sk.GetValue("Publisher"),
                                        UnninstallCommand = (string)sk.GetValue("UninstallString")
                                    });
                                }
                                catch (Exception)
                                { }
                            }

                        }
                    }
                }
            }
        }

        #region Downloader
        public DownloadService downloader;
        public string _tempLocation = string.Empty;
        private readonly string location = Path.GetTempPath() + @"\";

        public void DownloadWithIDM(string link)
        {
            string command = $"/C /d \"{link}\"";
            string IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
            string IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";
            if (File.Exists(IDManX64Location))
            {
                System.Diagnostics.Process.Start(IDManX64Location, command);
            }
            else if (File.Exists(IDManX86Location))
            {
                System.Diagnostics.Process.Start(IDManX86Location, command);
            }
            else
            {
                Growl.ErrorGlobal("Internet Download Manager (IDM) is not installed on your system, please download and install it first");
            }
        }

        public string GetExtension(string url)
        {
            var ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext))
            {
                string pointChar = ".";
                string slashChar = "/";

                int pointIndex = url.LastIndexOf(pointChar);
                int slashIndex = url.LastIndexOf(slashChar);

                if (pointIndex >= 0)
                {
                    if (slashIndex >= 0)
                    {
                        int pFrom = pointIndex + pointChar.Length;
                        int pTo = slashIndex;
                        return $".{url.Substring(pFrom, pTo - pFrom)}";
                    }
                    else
                    {
                        return url.Substring(pointIndex + pointChar.Length);

                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            else if (ext.Contains("?"))
            {
                int qTo = ext.IndexOf("?");
                return ext.Substring(0, qTo - 0);
            }
            else
            {
                return ext;
            }
        }

        public string RemoveComment(string url)
        {
            int index = url.IndexOf("#");
            if (index >= 0)
            {
                return url.Substring(0, index).Trim();
            }
            else
            {
                return url.Trim();
            }
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Progress = 0;
            IsVisibleProgressButton = false;
            IsCheckedProgressButton = true;
            DataGot = true;
            StartProcess(_tempLocation);
        }

        private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            var truncate = Math.Truncate(percentage);
            Progress = (int)truncate;
            LoadingStatus = $"Downloading {SelectedPackage.Id}-{SelectedPackage.Version}   {truncate}%";
        }
        #endregion

        class ItemEqualityComparer : IEqualityComparer<PackageModel>
        {
            public bool Equals(PackageModel x, PackageModel y)
            {
                // Two items are equal if their keys are equal.
                return x?.Name == y?.Name;
            }

            public int GetHashCode(PackageModel obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
