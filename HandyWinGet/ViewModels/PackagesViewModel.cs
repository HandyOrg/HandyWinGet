using Downloader;
using HandyControl.Controls;
using HandyWinGet.Data;
using HandyWinGet.Models;
using LibGit2Sharp;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MessageBox = HandyControl.Controls.MessageBox;

namespace HandyWinGet.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        public string _Id = string.Empty;
        private VersionModel SelectedPackage = new VersionModel();

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
            get { return _DataList; }
            set { SetProperty(ref _DataList, value); }
        }

        private ObservableCollection<VersionModel> _DataListVersion;
        public ObservableCollection<VersionModel> DataListVersion
        {
            get { return _DataListVersion; }
            set { SetProperty(ref _DataListVersion, value); }
        }

        private bool _DataGot;
        public bool DataGot
        {
            get { return _DataGot; }
            set { SetProperty(ref _DataGot, value); }
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
            get { return _LoadingStatus; }
            set { SetProperty(ref _LoadingStatus, value); }
        }

        private string _UpdatedDate;
        public string UpdatedDate
        {
            get { return _UpdatedDate; }
            set { SetProperty(ref _UpdatedDate, value); }
        }

        private int _Progress = 0;
        public int Progress
        {
            get { return _Progress; }
            set { SetProperty(ref _Progress, value); }
        }

        private bool _IsVisibleProgressButton;
        public bool IsVisibleProgressButton
        {
            get { return _IsVisibleProgressButton; }
            set { SetProperty(ref _IsVisibleProgressButton, value); }
        }

        private bool _IsCheckedProgressButton = true;
        public bool IsCheckedProgressButton
        {
            get { return _IsCheckedProgressButton; }
            set
            {
                SetProperty(ref _IsCheckedProgressButton, value);
                if (!value)
                {
                    downloader.CancelAsync();
                }
            }
        }
        #endregion

        private static object _lock = new object();

        public PackagesViewModel()
        {
            UpdatedDate = GlobalDataHelper<AppConfig>.Config.UpdatedDate.ToString();
            DataList = new ObservableCollection<PackageModel>();
            DataListVersion = new ObservableCollection<VersionModel>();
            BindingOperations.EnableCollectionSynchronization(DataList, _lock);
            BindingOperations.EnableCollectionSynchronization(DataListVersion, _lock);

            ItemsView.SortDescriptions.Add(new SortDescription("Company", ListSortDirection.Ascending));
            ItemsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            ItemsView.Filter = new Predicate<object>(o => Filter(o as PackageModel));
            ComboView.Filter = new Predicate<object>(o => FilterCombo(o as VersionModel));
            GetPackages();
        }

        #region Cloning Progress
        public bool RepositoryTransferProgress(TransferProgress progress)
        {
            LoadingStatus = $"{progress.ReceivedObjects} of {progress.TotalObjects}";
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

                foreach (string item in pkgs)
                {
                    string result = Regex.Replace(item, @".*(?=manifests)", "", RegexOptions.IgnorePatternWhitespace).Replace(@"manifests\", "");
                    string version = result.Substring(result.LastIndexOf('\\') + 1).Replace(".yaml", "").Replace(".Yaml", "").Trim();
                    string company = result.Substring(0, result.IndexOf('\\')).Trim();

                    int from = result.IndexOf(company) + company.Length + 1;
                    int to = result.LastIndexOf("\\");
                    string name = result.Substring(from, to - from).Trim();

                    string id = File.ReadAllLines(item).Where(l => l.Contains("Id:")).FirstOrDefault().Replace("Id:", "").Trim();
                    string url = File.ReadAllLines(item).Where(l => l.Contains("Url:", StringComparison.InvariantCultureIgnoreCase) && !l.Contains("LicenseUrl")).FirstOrDefault().Replace("Url:", "").Trim();

                    bool isInstalled = false;
                    var packge = new PackageModel { Company = company, Name = name, IsInstalled = isInstalled, Version = version, Id = id, Url = url };

                    if (!DataList.Contains(packge, new ItemEqualityComparer()))
                    {
                        DataList.Add(packge);
                    }
                    DataListVersion.Add(new VersionModel { Id = id, Version = version, Url = url });
                }

                CleanRepo();
            }).ContinueWith(obj =>
            {
                DataGot = true;
            });
        }

        class ItemEqualityComparer : IEqualityComparer<PackageModel>
        {
            public bool Equals(PackageModel x, PackageModel y)
            {
                // Two items are equal if their keys are equal.
                return x.Name == y.Name;
            }

            public int GetHashCode(PackageModel obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private bool Filter(PackageModel item)
        {
            return SearchText == null
                            || item.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1
                            || item.Company.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1;
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
                    if (GlobalDataHelper<AppConfig>.Config.IsIDM)
                    {
                        string url = RemoveComment(SelectedPackage.Url);
                        DownloadWithIDM(url);
                    }
                    else
                    {
                        IsVisibleProgressButton = true;
                        LoadingStatus = $"Preparing to download {SelectedPackage.Id}";
                        DataGot = false;

                        string url = RemoveComment(SelectedPackage.Url);

                        _tempLocation = $"{location} {SelectedPackage.Id} {GetExtension(url)}".Trim();
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
                Growl.ErrorGlobal(ex.Message);
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

                    return string.Empty;
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
    }
}
