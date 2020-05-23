using HandyControl.Controls;
using HandyWinget_GUI.Assets.Languages;
using HandyWinget_GUI.Models;
using LibGit2Sharp;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HandyWinget_GUI.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        //Todo: Create Pkg

        private readonly string path = Assembly.GetExecutingAssembly().Location.Replace(Path.GetFileName(Assembly.GetExecutingAssembly().Location), "") + @"pkgs";

        #region Property
        private ObservableCollection<PackagesModel> _dataList = new ObservableCollection<PackagesModel>();
        public ObservableCollection<PackagesModel> DataList
        {
            get => _dataList;
            set => SetProperty(ref _dataList, value);
        }

        private bool _IsEnabled = false;
        public bool IsEnabled
        {
            get => _IsEnabled;
            set => SetProperty(ref _IsEnabled, value);
        }

        private bool _IsEnabledRefresh = false;
        public bool IsEnabledRefresh
        {
            get => _IsEnabledRefresh;
            set => SetProperty(ref _IsEnabledRefresh, value);
        }

        private string _Status = "Ready";
        public string Status
        {
            get => _Status;
            set => SetProperty(ref _Status, value);
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

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        #endregion

        #region Commands
        public DelegateCommand InstallCommand { get; set; }
        public DelegateCommand RefreshCommand { get; set; }
        public DelegateCommand<SelectionChangedEventArgs> SwitchItemCmd { get; private set; }
        #endregion

        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(DataList);

        private PackagesModel SelectedPackage = new PackagesModel();

        public PackagesViewModel()
        {
            InstallCommand = new DelegateCommand(OnInstall);
            RefreshCommand = new DelegateCommand(OnRefresh);
            SwitchItemCmd = new DelegateCommand<SelectionChangedEventArgs>(Switch);

            GetDirectories();
            ItemsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            ItemsView.Filter = new Predicate<object>(o => Filter(o as PackagesModel));
        }

        private void OnInstall()
        {
            IsEnabled = false;
            IsEnabledRefresh = false;
            Status = string.Format(Lang.ResourceManager.GetString("StartedInstalling"), SelectedPackage.Name);

            Status = Lang.ResourceManager.GetString("StartInstall");

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
                    Status = Lang.ResourceManager.GetString("Downloading");
                }

                if (line.Contains("MB"))
                {
                    Status = string.Format(Lang.ResourceManager.GetString("DownloadingParameter"), line.Substring(94));
                }

                if (line.Contains("hash"))
                {
                    Status = string.Format(Lang.ResourceManager.GetString("Hash"), SelectedPackage.Name);
                }

                if (line.Contains("Installing"))
                {
                    Status = Lang.ResourceManager.GetString("Installing");
                }

                if (line.Contains("Failed"))
                {
                    Status = string.Format(Lang.ResourceManager.GetString("InstallFail"), SelectedPackage.Name);
                }
            };

            proc.Exited += (o, args) =>
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    string text = (o as Process).ExitCode != 0
                        ? string.Format(Lang.ResourceManager.GetString("InstallFailParameter"), SelectedPackage.Name)
                        : string.Format(Lang.ResourceManager.GetString("Installed"), SelectedPackage.Name);

                    Status = text;

                    IsEnabled = true;
                    IsEnabledRefresh = true;
                });
            };

            proc.Start();
            proc.BeginOutputReadLine();

        }

        private bool Filter(PackagesModel item)
        {
            return SearchText == null
                || item.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1;
        }

        private void OnRefresh()
        {
            DataList.Clear();
            GetDirectories();
        }

        private void Switch(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                SelectedPackage = null;
                return;
            }

            if (e.AddedItems[0] is PackagesModel item)
            {
                SelectedPackage = item;
                if (item.IsInstalled)
                {
                    IsEnabled = false;
                }
                else
                {
                    IsEnabled = true;
                }
            }
        }
        private void GetDirectories()
        {
            Tools.DeleteDirectory(path);
            IsEnabled = false;
            IsEnabledRefresh = false;
            IsBusy = true;
            CloneOptions cloneOptions = new CloneOptions();
            Task.Run(() =>
            {
                Repository.Clone("https://github.com/microsoft/winget-pkgs.git", path);

                IEnumerable<string> pkgs = GetAllDirectories(path + @"\manifests");
                foreach (string item in pkgs)
                {
                    string name = Regex.Replace(item, @".*(?=manifests)", "", RegexOptions.IgnorePatternWhitespace).Replace(@"manifests\", "");
                    string version = name.Substring(name.LastIndexOf('\\') + 1).Replace(".yaml", "").Replace(".Yaml", "").Trim();

                    int nameWithCompany = name.LastIndexOf('\\');
                    if (nameWithCompany > 0)
                    {
                        name = name.Substring(0, nameWithCompany).Replace("\\", " - ").Trim();
                    }

                    if (GlobalDataHelper<AppConfig>.Config.IsCheckedCompanyName)
                    {
                        name = name.Substring(name.IndexOf('-') + 1).Trim();
                    }

                    try
                    {
                        string id = File.ReadAllLines(item).Where(l => l.Contains("Id:")).FirstOrDefault().Replace("Id:", "").Trim();

                        bool isInstalled = false;
                        if (GlobalDataHelper<AppConfig>.Config.IsCheckAppInstalled)
                        {
                            isInstalled = Tools.IsSoftwareInstalled(id.Substring(id.IndexOf('.') + 1), version);
                        }
                        DataList.AddOnUI(new PackagesModel { Name = name, IsInstalled = isInstalled, Version = version, Id = id });
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                CleanRepo();
                IsBusy = false;
                IsEnabled = true;
                IsEnabledRefresh = true;
            });
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
            Tools.DeleteDirectory(path + @"\DevOpsPipelineDefinitions");
            Tools.DeleteDirectory(path + @"\Tools");
            Tools.DeleteDirectory(path + @"\.git");
            Tools.DeleteDirectory(path + @"\.github");

            string[] filePaths = System.IO.Directory.GetFiles(path);
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }
        }
    }
}
