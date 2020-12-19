using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Downloader;
using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Models;
using LibGit2Sharp;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using YamlDotNet.Serialization;
using ComboBox = HandyControl.Controls.ComboBox;
using MessageBox = HandyControl.Controls.MessageBox;

namespace HandyWinGet.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        internal static PackagesViewModel Instance;
        private static readonly object _lock = new();

        private readonly List<string> keys = new()
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        private readonly string path = Assembly.GetExecutingAssembly().Location
            .Replace(Path.GetFileName(Assembly.GetExecutingAssembly().Location), "") + @"pkgs";

        public string _Id = string.Empty;

        private List<InstalledAppModel> InstalledApps = new();
        private VersionModel SelectedPackage = new();

        private string wingetData = string.Empty;

        public PackagesViewModel()
        {
            Instance = this;
            UpdatedDate = GlobalDataHelper<AppConfig>.Config.UpdatedDate.ToString();
            DataList = new ObservableCollection<PackageModel>();
            DataListVersion = new ObservableCollection<VersionModel>();
            BindingOperations.EnableCollectionSynchronization(DataList, _lock);
            BindingOperations.EnableCollectionSynchronization(DataListVersion, _lock);
            SetDataGridGrouping();
            ItemsView.Filter = o => Filter(o as PackageModel);
            ComboView.Filter = o => FilterCombo(o as VersionModel);
            GetPackages();
        }

        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(DataList);
        public ICollectionView ComboView => CollectionViewSource.GetDefaultView(DataListVersion);

        public void SetDataGridGrouping()
        {
            if (GlobalDataHelper<AppConfig>.Config.IsShowingGroup)
                ItemsView.GroupDescriptions.Add(new PropertyGroupDescription("Publisher"));
            else
                ItemsView.GroupDescriptions.Clear();
        }

        public void CloneRepository()
        {
            DeleteDirectory(path);

            var cloneOptions = new CloneOptions();
            cloneOptions.RepositoryOperationStarting = RepositoryOperationStartingProgress;
            cloneOptions.OnCheckoutProgress = RepositoryOnCheckoutProgress;
            cloneOptions.OnTransferProgress = RepositoryTransferProgress;
            Repository.Clone("https://github.com/microsoft/winget-pkgs.git", path, cloneOptions);
            UpdatedDate = DateTime.Now.ToString();
            GlobalDataHelper<AppConfig>.Config.UpdatedDate = DateTime.Now;
            GlobalDataHelper<AppConfig>.Save();
        }


        private void GetPackages(bool ForceUpdate = false)
        {
            Task.Run(() =>
            {
                if (!Directory.Exists(path + @"\manifests")) CloneRepository();

                if (ForceUpdate) CloneRepository();

                LoadingStatus = "Extracting packages...";

                var pkgs = GetAllDirectories(path + @"\manifests").OrderByDescending(x => x).ToList();
                FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), keys,
                    InstalledApps);
                FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64), keys,
                    InstalledApps);

                InstalledApps = InstalledApps.Distinct().ToList();
                foreach (var item in pkgs)
                {
                    var file = File.ReadAllText(item);
                    var input = new StringReader(file);

                    var deserializer = new DeserializerBuilder().Build();
                    var yamlObject = deserializer.Deserialize(input);
                    var serializer = new SerializerBuilder()
                        .JsonCompatible()
                        .Build();

                    var json = serializer.Serialize(yamlObject);
                    var yaml = JsonSerializer.Deserialize<YamlModel>(json);

                    var installedVersion = string.Empty;
                    var isInstalled = false;

                    switch (GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode)
                    {
                        case IdentifyPackageMode.Off:
                            isInstalled = false;
                            break;
                        case IdentifyPackageMode.Internal:
                            foreach (var itemApp in InstalledApps)
                                if (itemApp.DisplayName.Contains(yaml.Name))
                                {
                                    installedVersion = $"Installed Version: {itemApp.Version}";
                                    isInstalled = true;
                                }

                            break;
                        case IdentifyPackageMode.Wingetcli:
                            isInstalled = IsWingetcliPackageInstalled(yaml.Name);
                            break;
                    }

                    var packge = new PackageModel
                    {
                        Publisher = yaml.Publisher,
                        Name = yaml.Name,
                        IsInstalled = isInstalled,
                        Version = yaml.Version,
                        Id = yaml.Id,
                        Url = yaml.Installers[0].Url,
                        Description = yaml.Description,
                        LicenseUrl = yaml.LicenseUrl,
                        Homepage = yaml.Homepage,
                        Arch = yaml.Id + " " + yaml.Installers[0].Arch,
                        InstalledVersion = installedVersion
                    };

                    if (!DataList.Contains(packge, new ItemEqualityComparer())) DataList.Add(packge);

                    DataListVersion.Add(new VersionModel
                        {Id = yaml.Id, Version = yaml.Version, Url = yaml.Installers[0].Url});
                }

                CleanRepo();
            }).ContinueWith(obj => { DataGot = true; });
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

        private bool IsWingetcliPackageInstalled(string packageName)
        {
            if (string.IsNullOrEmpty(wingetData))
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "winget",
                        Arguments = "list"
                    }
                };

                p.Start();
                wingetData = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            if (wingetData.Contains("Unrecognized command"))
            {
                Growl.ErrorGlobal("your Winget-cli is not supported please Update your winget-cli.");
                Process.Start("https://github.com/microsoft/winget-cli/releases");
                return false;
            }

            return wingetData.Contains(packageName);
        }

        private void ItemChanged(SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is DataGrid dg)
            {
                var dgItem = (PackageModel) dg.SelectedItem;

                if (dgItem != null)
                {
                    _Id = dgItem.Id;
                    ComboView.Refresh();
                    SelectedPackage = new VersionModel {Id = dgItem.Id, Version = dgItem.Version, Url = dgItem.Url};
                }
            }

            if (e.OriginalSource is ComboBox cmb)
            {
                var cmbItem = (VersionModel) cmb.SelectedItem;
                if (cmbItem != null)
                    SelectedPackage = new VersionModel {Id = cmbItem.Id, Version = cmbItem.Version, Url = cmbItem.Url};
            }
        }

        private void OnButtonAction(string param)
        {
            switch (param)
            {
                case "Install":
                    switch (GlobalDataHelper<AppConfig>.Config.InstallMode)
                    {
                        case InstallMode.Wingetcli:
                            if (((App) Application.Current).IsWingetInstalled())
                            {
                                InstallWingetMode();
                            }
                            else
                            {
                                MessageBox.Error(
                                    "Winget-cli is not installed, please download and install latest version.",
                                    "Install Winget");
                                StartProcess("https://github.com/microsoft/winget-cli/releases");
                            }

                            break;
                        case InstallMode.Internal:
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

        public void InstallWingetMode()
        {
            if (SelectedPackage.Id != null)
            {
                DataGot = false;
                LoadingStatus = $"Preparing to download {SelectedPackage.Id}";

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install {SelectedPackage.Id} {$"-v {SelectedPackage.Version}"}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };

                proc.OutputDataReceived += (o, args) =>
                {
                    var line = args.Data ?? "";

                    if (line.Contains("Download")) LoadingStatus = "Downloading Package...";

                    if (line.Contains("hash")) LoadingStatus = $"Validated hash for {SelectedPackage.Id}";

                    if (line.Contains("Installing")) LoadingStatus = $"Installing {SelectedPackage.Id}";

                    if (line.Contains("Failed")) LoadingStatus = $"Installation of {SelectedPackage.Id} failed";
                };

                proc.Exited += (o, args) =>
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var installFailed = (o as Process).ExitCode != 0;
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
                    var url = RemoveComment(SelectedPackage.Url);

                    if (GlobalDataHelper<AppConfig>.Config.IsIDMEnabled)
                    {
                        DownloadWithIDM(url);
                    }
                    else
                    {
                        IsVisibleProgressButton = true;
                        LoadingStatus = $"Preparing to download {SelectedPackage.Id}";
                        DataGot = false;

                        _tempLocation = $"{location}{SelectedPackage.Id}-{SelectedPackage.Version}{GetExtension(url)}"
                            .Trim();
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
                var ps = new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Win32Exception ex)
            {
                if (!ex.Message.Contains("The system cannot find the file specified.")) Growl.ErrorGlobal(ex.Message);
            }
        }

        private void FindInstalledApps(RegistryKey regKey, List<string> keys, List<InstalledAppModel> installed)
        {
            foreach (var key in keys)
                using (var rk = regKey.OpenSubKey(key))
                {
                    if (rk == null) continue;

                    foreach (var skName in rk.GetSubKeyNames())
                        using (var sk = rk.OpenSubKey(skName))
                        {
                            if (sk.GetValue("DisplayName") != null)
                                try
                                {
                                    installed.Add(new InstalledAppModel
                                    {
                                        DisplayName = (string) sk.GetValue("DisplayName"),
                                        Version = (string) sk.GetValue("DisplayVersion"),
                                        Publisher = (string) sk.GetValue("Publisher"),
                                        UnninstallCommand = (string) sk.GetValue("UninstallString")
                                    });
                                }
                                catch (Exception)
                                {
                                }
                        }
                }
        }

        private class ItemEqualityComparer : IEqualityComparer<PackageModel>
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

        private int _Progress;

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
                if (!value) downloader.CancelAsync();
            }
        }

        private DataGridRowDetailsVisibilityMode _RowDetailsVisibilityMode;

        public DataGridRowDetailsVisibilityMode RowDetailsVisibilityMode
        {
            get => GlobalDataHelper<AppConfig>.Config.IsShowingExtraDetail
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                : DataGridRowDetailsVisibilityMode.Collapsed;
            set => SetProperty(ref _RowDetailsVisibilityMode, value);
        }

        #endregion

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

        #region Clean Repo

        public IEnumerable<string> GetAllDirectories(string rootDirectory)
        {
            foreach (var directory in Directory.GetDirectories(
                rootDirectory,
                "*",
                SearchOption.AllDirectories))
            foreach (var file in Directory.GetFiles(directory))
                yield return file;
        }

        private void CleanRepo()
        {
            DeleteDirectory(path + @"\DevOpsPipelineDefinitions");
            DeleteDirectory(path + @"\Tools");
            DeleteDirectory(path + @"\.git");
            DeleteDirectory(path + @"\.github");

            var filePaths = Directory.GetFiles(path);
            foreach (var filePath in filePaths) File.Delete(filePath);
        }

        public static void DeleteDirectory(string d)
        {
            if (Directory.Exists(d))
            {
                foreach (var sub in Directory.EnumerateDirectories(d)) DeleteDirectory(sub);

                foreach (var f in Directory.EnumerateFiles(d))
                {
                    var fi = new FileInfo(f)
                    {
                        Attributes = FileAttributes.Normal
                    };
                    fi.Delete();
                }

                Directory.Delete(d);
            }
        }

        #endregion

        #region Downloader

        public DownloadService downloader;
        public string _tempLocation = string.Empty;
        private readonly string location = Path.GetTempPath() + @"\";

        public void DownloadWithIDM(string link)
        {
            var command = $"/C /d \"{link}\"";
            var IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
            var IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";
            if (File.Exists(IDManX64Location))
                Process.Start(IDManX64Location, command);
            else if (File.Exists(IDManX86Location))
                Process.Start(IDManX86Location, command);
            else
                Growl.ErrorGlobal(
                    "Internet Download Manager (IDM) is not installed on your system, please download and install it first");
        }

        public string GetExtension(string url)
        {
            var ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext))
            {
                var pointChar = ".";
                var slashChar = "/";

                var pointIndex = url.LastIndexOf(pointChar);
                var slashIndex = url.LastIndexOf(slashChar);

                if (pointIndex >= 0)
                {
                    if (slashIndex >= 0)
                    {
                        var pFrom = pointIndex + pointChar.Length;
                        var pTo = slashIndex;
                        return $".{url.Substring(pFrom, pTo - pFrom)}";
                    }

                    return url.Substring(pointIndex + pointChar.Length);
                }

                return string.Empty;
            }

            if (ext.Contains("?"))
            {
                var qTo = ext.IndexOf("?");
                return ext.Substring(0, qTo - 0);
            }

            return ext;
        }

        public string RemoveComment(string url)
        {
            var index = url.IndexOf("#");
            if (index >= 0) return url.Substring(0, index).Trim();

            return url.Trim();
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Progress = 0;
            IsVisibleProgressButton = false;
            IsCheckedProgressButton = true;
            DataGot = true;
            StartProcess(_tempLocation);
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var bytesIn = double.Parse(e.BytesReceived.ToString());
            var totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            var percentage = bytesIn / totalBytes * 100;
            var truncate = Math.Truncate(percentage);
            Progress = (int) truncate;
            LoadingStatus = $"Downloading {SelectedPackage.Id}-{SelectedPackage.Version}   {truncate}%";
        }

        #endregion
    }
}