using Downloader;
using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Models;
using HandyWinGet.Views;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YamlDotNet.Serialization;
using ComboBox = HandyControl.Controls.ComboBox;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;
using MessageBox = HandyControl.Controls.MessageBox;

namespace HandyWinGet.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        #region Command

        private DelegateCommand<string> _buttonCmd;
        private DelegateCommand<SelectionChangedEventArgs> _itemChangedCmd;

        public DelegateCommand<string> ButtonCmd =>
            _buttonCmd ??= new DelegateCommand<string>(OnButtonAction);

        public DelegateCommand<SelectionChangedEventArgs> ItemChangedCmd =>
            _itemChangedCmd ??= new DelegateCommand<SelectionChangedEventArgs>(ItemChanged);

        #endregion

        #region Property

        private ObservableCollection<PackageModel> _dataList;
        private ObservableCollection<VersionModel> _dataListVersion;
        private DataGridRowDetailsVisibilityMode _rowDetailsVisibilityMode;
        private bool _dataGot;
        private string _searchText;
        private string _loadingStatus;
        private string _updatedDate;
        private int _progress;
        private bool _isVisibleProgressButton;
        private bool _isIndeterminate;
        private bool _isShowError;
        private bool _isShowOnlyInstalledApps;
        private bool _isCheckedProgressButton = true;

        public ObservableCollection<PackageModel> DataList
        {
            get => _dataList;
            set => SetProperty(ref _dataList, value);
        }

        public ObservableCollection<VersionModel> DataListVersion
        {
            get => _dataListVersion;
            set => SetProperty(ref _dataListVersion, value);
        }

        public bool DataGot
        {
            get => _dataGot;
            set => SetProperty(ref _dataGot, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                if (IsShowOnlyInstalledApps)
                {
                    DataList.ShapeView()
                        .Where(x =>
                            (x.IsInstalled && x.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1) ||
                            (x.IsInstalled && x.Publisher.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) !=
                                -1)).Apply();
                }
                else
                {
                    DataList.ShapeView()
                        .Where(x => x.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1 ||
                                    x.Publisher.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1).Apply();
                }
            }
        }

        public string LoadingStatus
        {
            get => _loadingStatus;
            set => SetProperty(ref _loadingStatus, value);
        }

        public string UpdatedDate
        {
            get => _updatedDate;
            set => SetProperty(ref _updatedDate, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public bool IsVisibleProgressButton
        {
            get => _isVisibleProgressButton;
            set => SetProperty(ref _isVisibleProgressButton, value);
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }

        public bool IsShowError
        {
            get => _isShowError;
            set => SetProperty(ref _isShowError, value);
        }

        public bool IsShowOnlyInstalledApps
        {
            get => _isShowOnlyInstalledApps;
            set
            {
                SetProperty(ref _isShowOnlyInstalledApps, value);
                if (value)
                {
                    DataList.ShapeView().Where(x => x.IsInstalled).Apply();
                }
                else
                {
                    DataList.ShapeView().ClearFilter().Apply();
                }
            }
        }

        public bool IsCheckedProgressButton
        {
            get => _isCheckedProgressButton;
            set
            {
                SetProperty(ref _isCheckedProgressButton, value);
                if (!value)
                {
                    DownloaderService.CancelAsync();
                    DataGot = true;
                }
            }
        }

        public DataGridRowDetailsVisibilityMode RowDetailsVisibilityMode
        {
            get => GlobalDataHelper<AppConfig>.Config.IsShowingExtraDetail
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                : DataGridRowDetailsVisibilityMode.Collapsed;
            set => SetProperty(ref _rowDetailsVisibilityMode, value);
        }

        #endregion

        internal static PackagesViewModel Instance;
        public DownloadService DownloaderService;
        public ICollectionView ComboView => CollectionViewSource.GetDefaultView(DataListVersion);
        private List<InstalledAppModel> _installedApps = new();
        private VersionModel _selectedPackage = new();

        private readonly List<string> _keys = new()
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        private readonly string _path =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HandyWinGet");

        private string _wingetData = string.Empty;

        private static readonly object Lock = new();
        public string Id = string.Empty;
        public string TempLocation = string.Empty;

        private readonly string _connectionErrorMessage =
            @$"Unable to connect to the Internet {Environment.NewLine} HandyWinGet can't download manifests because your computer isn't connected to the Internet.";

        public PackagesViewModel()
        {
            Instance = this;
            UpdatedDate = GlobalDataHelper<AppConfig>.Config.UpdatedDate.ToString();
            DataList = new ObservableCollection<PackageModel>();
            DataListVersion = new ObservableCollection<VersionModel>();
            BindingOperations.EnableCollectionSynchronization(DataList, Lock);
            BindingOperations.EnableCollectionSynchronization(DataListVersion, Lock);
            SetDataGridGrouping();
            ComboView.Filter = o => FilterCombo(o as VersionModel);
            GetPackages();
        }

        private void GetPackages(bool isRefresh = false)
        {
            if (Tools.IsConnectedToInternet() || Directory.Exists(_path + @"\manifests"))
            {
                Task.Run(async () =>
                {
                    if (!Directory.Exists(_path + @"\manifests") || isRefresh)
                    {
                        var manifestUrl = "https://github.com/microsoft/winget-pkgs/archive/master.zip";

                        DownloaderService = new DownloadService();
                        DownloaderService.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                        {
                            OnDownloadProgressChanged(sender, e, DownloadMode.Repository);
                        };
                        DownloaderService.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
                        {
                            OnDownloadFileCompleted(sender, e, DownloadMode.Repository);
                        };
                        await DownloaderService.DownloadFileAsync(manifestUrl, new DirectoryInfo(_path));
                    }

                    LoadingStatus = "Extracting packages...";

                    var pkgs = Tools.EnumerateManifest(_path + @"\manifests").OrderByDescending(x => x);

                    Tools.FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
                        _keys,
                        _installedApps);
                    Tools.FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
                        _keys,
                        _installedApps);
                    _installedApps = _installedApps.Distinct().ToList();

                    int totalPackageCount = pkgs.Count();
                    int checkedPackageCount = 0;
                    foreach (var item in pkgs)
                    {
                        checkedPackageCount += 1;
                        Progress = checkedPackageCount * 100 / totalPackageCount;

                        var file = File.ReadAllText(item);
                        var input = new StringReader(file);

                        var deserializer = new DeserializerBuilder().Build();
                        var yamlObject = deserializer.Deserialize(input);
                        var serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

                        if (yamlObject != null)
                        {
                            var json = serializer.Serialize(yamlObject);
                            var yaml = JsonSerializer.Deserialize<YamlModel>(json);

                            if (yaml != null)
                            {
                                var installedVersion = string.Empty;
                                var isInstalled = false;
                                switch (GlobalDataHelper<AppConfig>.Config.IdentifyPackageMode)
                                {
                                    case IdentifyPackageMode.Off:
                                        isInstalled = false;
                                        break;
                                    case IdentifyPackageMode.Internal:
                                        foreach (var itemApp in _installedApps)
                                        {
                                            if (itemApp.DisplayName.Contains(yaml.Name))
                                            {
                                                installedVersion = $"Installed Version: {itemApp.Version}";
                                                isInstalled = true;
                                            }
                                        }

                                        break;
                                    case IdentifyPackageMode.Wingetcli:
                                        isInstalled = await IsWingetcliPackageInstalled(yaml.Name);
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

                                if (!DataList.Contains(packge, new ItemEqualityComparer()))
                                {
                                    DataList.Add(packge);
                                }

                                DataListVersion.Add(new VersionModel
                                { Id = yaml.Id, Version = yaml.Version, Url = yaml.Installers[0].Url });
                            }
                        }
                    }
                }).ContinueWith(obj => { DataGot = true; });
            }
            else
            {
                DataGot = true;
                DownloaderService.CancelAsync();
                Growl.ErrorGlobal(_connectionErrorMessage);
            }
        }

        private async void OnButtonAction(string param)
        {
            string text = $"winget install {_selectedPackage.Id} -v {_selectedPackage.Version}";

            switch (param)
            {
                case "Install":
                    if (Tools.IsConnectedToInternet())
                    {
                        switch (GlobalDataHelper<AppConfig>.Config.InstallMode)
                        {
                            case InstallMode.Wingetcli:
                                if (Tools.IsWingetInstalled())
                                {
                                    InstallWingetMode();
                                }
                                else
                                {
                                    MessageBox.Error(
                                        "Winget-cli is not installed, please download and install latest version.",
                                        "Install Winget");
                                    Tools.StartProcess("https://github.com/microsoft/winget-cli/releases");
                                }

                                break;
                            case InstallMode.Internal:
                                if (Packages.Instance.dg.SelectedItems.Count > 1)
                                {
                                    MessageBox.Error(
                                        "you can not install more than 1 package in Internal Mode, for doing this please go to settings and switch Install Mode from Internal to Winget-cli Mode.",
                                        "Limited Behavior");
                                }
                                else
                                {
                                    InstallInternalMode();
                                }

                                break;
                        }
                    }
                    else
                    {
                        Growl.ErrorGlobal(_connectionErrorMessage);
                    }

                    break;
                case "PowerShell":
                    if (Packages.Instance.dg.SelectedItems.Count > 0)
                    {
                        var dialog = new SaveFileDialog
                        {
                            Title = "Save Script",
                            FileName = "winget-script.ps1",
                            DefaultExt = "ps1",
                            Filter = "Powershell Script (*.ps1)|*.ps1"
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            await File.WriteAllTextAsync(dialog.FileName, CreatePowerShellScript(true).Item1);
                        }
                    }
                    else
                    {
                        MessageBox.Info("Please Selected Packages", "Select Package");
                    }

                    break;
                case "Refresh":
                    IsIndeterminate = true;
                    LoadingStatus = "Refreshing Packages...";
                    DataList.Clear();
                    DataListVersion.Clear();
                    _installedApps.Clear();
                    DataGot = false;
                    GetPackages(true);
                    break;
                case "Copy":
                    Clipboard.SetText(text);
                    break;
                case "Uninstall":
                    if (!string.IsNullOrEmpty(_selectedPackage.DisplayName) && ((PackageModel)(Packages.Instance.dg.SelectedItem)).IsInstalled)
                    {
                        var result = Tools.UninstallPackage(_selectedPackage.DisplayName);
                        if (!result)
                        {
                            Growl.InfoGlobal("Sorry, we were unable to uninstall your package");
                        }
                    }
                    break;
                case "SendToPow":
                    if (Packages.Instance.dg.SelectedItems.Count > 1)
                    {
                        var script = CreatePowerShellScript(false);
                        Process.Start("powershell.exe", script.Item1);
                    }
                    else
                    {
                        Process.Start("powershell.exe", text);
                    }
                    break;
                case "SendToCmd":
                    Interaction.Shell(text, AppWinStyle.NormalFocus);
                    break;
            }
        }

        private async Task<bool> IsWingetcliPackageInstalled(string packageName)
        {
            if (string.IsNullOrEmpty(_wingetData))
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
                _wingetData = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
            }

            if (_wingetData.Contains("Unrecognized command"))
            {
                Growl.ErrorGlobal("your Winget-cli is not supported please Update your winget-cli.");
                Process.Start("https://github.com/microsoft/winget-cli/releases");
                return false;
            }

            return _wingetData.Contains(packageName);
        }

        private void ItemChanged(SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is DataGrid dg)
            {
                var dgItem = (PackageModel)dg.SelectedItem;

                if (dgItem != null)
                {
                    Id = dgItem.Id;
                    ComboView.Refresh();
                    _selectedPackage = new VersionModel { Id = dgItem.Id, Version = dgItem.Version, Url = dgItem.Url, DisplayName = dgItem.Name };
                }
            }

            if (e.OriginalSource is ComboBox cmb)
            {
                var cmbItem = (VersionModel)cmb.SelectedItem;
                if (cmbItem != null)
                {
                    _selectedPackage = new VersionModel { Id = cmbItem.Id, Version = cmbItem.Version, Url = cmbItem.Url, DisplayName = cmbItem.DisplayName };
                }
            }
        }

        private void InstallWingetMode()
        {
            if (_selectedPackage.Id != null)
            {
                DataGot = false;
                IsIndeterminate = true;
                LoadingStatus = $"Preparing to download {_selectedPackage.Id}";
                int selectedPackagesCount = 1;
                int currentCount = 0;
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                if (Packages.Instance.dg.SelectedItems.Count > 1)
                {
                    var script = CreatePowerShellScript(false);
                    selectedPackagesCount = script.Item2;
                    startInfo.FileName = @"powershell.exe";
                    startInfo.Arguments = script.Item1;
                }
                else
                {
                    startInfo.FileName = @"winget";
                    startInfo.Arguments = $"install {_selectedPackage.Id} -v {_selectedPackage.Version}";
                }

                var proc = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                proc.OutputDataReceived += (o, args) =>
                {
                    var line = args.Data ?? "";

                    if (line.Contains("Download"))
                    {
                        currentCount += 1;
                        LoadingStatus = $"Downloading Package {currentCount}/{selectedPackagesCount}...";
                    }

                    if (line.Contains("hash"))
                    {
                        LoadingStatus =
                            $"Validated hash for {_selectedPackage.Id} {currentCount}/{selectedPackagesCount}";
                    }

                    if (line.Contains("Installing"))
                    {
                        LoadingStatus = $"Installing {_selectedPackage.Id} {currentCount}/{selectedPackagesCount}";
                    }

                    if (line.Contains("Failed"))
                    {
                        LoadingStatus = $"Installation of {_selectedPackage.Id} failed";
                    }
                };

                proc.Exited += (o, _) =>
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        var installFailed = (o as Process).ExitCode != 0;
                        if (installFailed)
                        {
                            LoadingStatus = $"Installation of {_selectedPackage.Id} failed";
                            IsShowError = true;
                        }
                        else
                        {
                            LoadingStatus = $"Installed {_selectedPackage.Id}";
                            IsIndeterminate = false;
                        }

                        await Task.Delay(4500);
                        DataGot = true;
                        IsShowError = false;
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
                if (_selectedPackage.Id != null)
                {
                    var url = Tools.RemoveComment(_selectedPackage.Url);

                    if (GlobalDataHelper<AppConfig>.Config.IsIDMEnabled)
                    {
                        Tools.DownloadWithIDM(url);
                    }
                    else
                    {
                        IsVisibleProgressButton = true;
                        LoadingStatus = $"Preparing to download {_selectedPackage.Id}";
                        DataGot = false;

                        TempLocation =
                            $@"{Path.GetTempPath()}\{_selectedPackage.Id}-{_selectedPackage.Version}{Tools.GetExtension(url)}"
                                .Trim();
                        if (!File.Exists(TempLocation))
                        {
                            DownloaderService = new DownloadService();
                            DownloaderService.DownloadProgressChanged +=
                                delegate (object sender, DownloadProgressChangedEventArgs e)
                                {
                                    OnDownloadProgressChanged(sender, e, DownloadMode.Package);
                                };
                            DownloaderService.DownloadFileCompleted +=
                                delegate (object sender, AsyncCompletedEventArgs e)
                                {
                                    OnDownloadFileCompleted(sender, e, DownloadMode.Package);
                                };
                            await DownloaderService.DownloadFileAsync(url, TempLocation);
                        }
                        else
                        {
                            IsVisibleProgressButton = false;
                            DataGot = true;
                            Tools.StartProcess(TempLocation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        private void CleanDirectory()
        {
            var rootDir = new DirectoryInfo(_path + @"\winget-pkgs-master");
            var zipFile = new FileInfo(DownloaderService.Package.FileName);
            var pkgDir = new DirectoryInfo(_path + @"\manifests");
            var moveDir = new DirectoryInfo(_path + @"\winget-pkgs-master\manifests");
            if (moveDir.Exists)
            {
                if (pkgDir.Exists)
                {
                    pkgDir.Delete(true);
                }

                moveDir.MoveTo(pkgDir.FullName);
                rootDir.Delete(true);

                if (zipFile.Exists)
                {
                    zipFile.Delete();
                }
            }
        }

        public void SetDataGridGrouping()
        {
            if (GlobalDataHelper<AppConfig>.Config.IsShowingGroup)
            {
                DataList.ShapeView().GroupBy(x => x.Publisher).Apply();
            }
            else
            {
                DataList.ShapeView().ClearGrouping().Apply();
            }
        }

        private bool FilterCombo(VersionModel item)
        {
            return Id == null || item.Id.Equals(Id);
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e, DownloadMode mode)
        {
            switch (mode)
            {
                case DownloadMode.Repository:
                    try
                    {
                        UpdatedDate = DateTime.Now.ToString();
                        GlobalDataHelper<AppConfig>.Config.UpdatedDate = DateTime.Now;
                        GlobalDataHelper<AppConfig>.Save();
                        IsIndeterminate = true;
                        LoadingStatus = "Extracting Manifests...";
                        ZipFile.ExtractToDirectory(DownloaderService.Package.FileName, _path, true);
                        LoadingStatus = "Cleaning Directory...";
                        CleanDirectory();
                        Progress = 0;
                        IsIndeterminate = false;
                    }
                    catch (InvalidDataException)
                    {
                    }
                    CleanDirectory();
                    break;
                case DownloadMode.Package:
                    IsVisibleProgressButton = false;
                    IsCheckedProgressButton = true;
                    Tools.StartProcess(TempLocation);
                    break;
            }

            Progress = 0;
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e, DownloadMode mode)
        {
            Progress = (int)e.ProgressPercentage;
            switch (mode)
            {
                case DownloadMode.Repository:

                    if (e.TotalBytesToReceive == -1)
                    {
                        if (!IsIndeterminate)
                        {
                            IsIndeterminate = true;
                        }

                        LoadingStatus = "Downloading...";
                    }
                    else
                    {
                        if (IsIndeterminate)
                        {
                            IsIndeterminate = false;
                        }

                        Progress = Progress;
                        LoadingStatus =
                            $"Downloading {Tools.ConvertBytesToMegabytes(e.BytesReceived)} MB of {Tools.ConvertBytesToMegabytes(e.TotalBytesToReceive)} MB  -  {Progress}%";
                    }
                    break;
                case DownloadMode.Package:
                    LoadingStatus =
                        $"Downloading {_selectedPackage.Id}-{_selectedPackage.Version} - {Tools.ConvertBytesToMegabytes(e.BytesReceived)} MB of {Tools.ConvertBytesToMegabytes(e.TotalBytesToReceive)} MB  -   {Progress}%";
                    break;
            }
        }

        private (string, int) CreatePowerShellScript(bool isExportScript)
        {
            StringBuilder builder = new StringBuilder();
            if (isExportScript)
            {
                builder.Append(Tools.PowerShellScript);
            }

            foreach (var item in Packages.Instance.dg.SelectedItems)
            {
                builder.Append($"winget install {((PackageModel)item).Id} -v {((PackageModel)item).Version} -e ; ");
            }

            builder.Remove(builder.ToString().LastIndexOf(";"), 1);
            if (isExportScript)
            {
                builder.AppendLine("}");
            }

            return (builder.ToString().TrimEnd(), Packages.Instance.dg.SelectedItems.Count);
        }

        private enum DownloadMode
        {
            Repository,
            Package
        }
    }
}