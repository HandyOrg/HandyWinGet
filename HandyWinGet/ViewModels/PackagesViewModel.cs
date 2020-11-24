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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace HandyWinGet.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(DataList);
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
        public ObservableCollection<VersionModel> _temp = new ObservableCollection<VersionModel>();

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
        #endregion


        private static object _lock = new object();

        public PackagesViewModel()
        {

            DataList = new ObservableCollection<PackageModel>();
            DataListVersion = new ObservableCollection<VersionModel>();
            BindingOperations.EnableCollectionSynchronization(DataList, _lock);

            ItemsView.Filter = new Predicate<object>(o => Filter(o as PackageModel));
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
                    string company = result.Substring(0, result.IndexOf('\\') - 1).Trim();

                    int from = result.IndexOf(company) + company.Length + 2;
                    int to = result.LastIndexOf("\\");
                    string name = result.Substring(from, to - from).Trim();

                    string id = File.ReadAllLines(item).Where(l => l.Contains("Id:")).FirstOrDefault().Replace("Id:", "").Trim();

                    bool isInstalled = false;
                    var packge = new PackageModel { Company = company, Name = name, IsInstalled = isInstalled, Version = version, Id = id };

                    if (!DataList.Contains(packge, new ItemEqualityComparer()))
                    {
                        DataList.Add(packge);
                    }

                    _temp.Add(new VersionModel { Id = id, Version = version });
                }

                CleanRepo();
            }).ContinueWith(obj =>
            {
                DataGot = true;
                GlobalDataHelper<AppConfig>.Config.UpdatedDate = DateTime.Now;
                UpdatedDate = DateTime.Now.ToString();
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
                            || item.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1;
        }

        void ItemChanged(SelectionChangedEventArgs e)
        {
            try
            {
                if (e.OriginalSource is DataGrid)
                {
                    DataListVersion?.Clear();

                    if (e.AddedItems[0] is PackageModel item)
                    {
                        var result = _temp.Where(w => w.Id.Equals(item.Id)).Select(x => new { x.Id, x.Version }).OrderByDescending(x => x.Version).ToList();

                        foreach (var res in result)
                        {
                            DataListVersion.Add(new VersionModel { Id = res.Id, Version = res.Version });

                        }
                    }
                }
            }
            catch (IndexOutOfRangeException) { }
        }

        void OnButtonAction(string param)
        {
            switch (param)
            {
                case "Install":
                    break;
                case "Uninstall":
                    break;
                case "Refresh":
                    LoadingStatus = "Refreshing Packages...";
                    DataList.Clear();
                    DataListVersion.Clear();
                    _temp.Clear();
                    DataGot = false;
                    GetPackages(true);
                    break;
            }
        }

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

    }
}
