using HandyControl.Controls;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WinGet_GUI.Assets.Languages;
using WinGet_GUI.Models;
namespace WinGet_GUI.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        //Todo: Check if Installed
        //Todo: Create Pkg

        private readonly string path = @"pkgs";

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

        private string _Status = "Status";
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
            }
        }
        private void GetDirectories()
        {
            Tools.DeleteDirectory(path);
            IsEnabled = false;
            IsBusy = true;
            CloneOptions cloneOptions = new CloneOptions();
            Task.Run(() =>
            {
                Repository.Clone("https://github.com/microsoft/winget-pkgs.git", path);

                IEnumerable<string> pkgs = GetAllDirectories(path + @"\manifests");
                foreach (string item in pkgs)
                {
                    string fixName = item.Replace(@"pkgs\manifests\", "");
                    int index = fixName.LastIndexOf('.');
                    if (index > 0)
                    {
                        fixName = fixName.Substring(0, index).Replace("\\", " - ").Trim();
                    }

                    string ver = fixName.Substring(fixName.LastIndexOf('-') + 1).Trim();
                    if (GlobalDataHelper<AppConfig>.Config.IsCheckedCompanyName)
                    {
                        fixName = fixName.Substring(fixName.IndexOf('-') + 1).Trim();
                    }
                    try
                    {
                        string id = File.ReadAllLines(item).Where(l => l.Contains("Id:")).FirstOrDefault().Replace("Id:", "").Trim();

                        //Todo: check if installed
                        DataList.AddOnUI(new PackagesModel { Name = fixName, IsInstalled = true, Version = ver, Id = id });
                    }
                    catch (InvalidOperationException)
                    {
                    }

                }

                CleanRepo();
                IsBusy = false;
                IsEnabled = true;
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

            string[] filePaths = System.IO.Directory.GetFiles(@"pkgs");
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }
        }
    }
}
