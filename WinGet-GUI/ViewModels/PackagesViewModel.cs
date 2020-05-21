using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        #region Property
        private ObservableCollection<ApplicationData> _dataList = new ObservableCollection<ApplicationData>();
        public ObservableCollection<ApplicationData> DataList
        {
            get => _dataList;
            set => SetProperty(ref _dataList, value);
        }

        private bool _IsEnabled = true;
        public bool IsEnabled
        {
            get => _IsEnabled = true;
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

        private ApplicationData SelectedPackage = new ApplicationData();

        public PackagesViewModel()
        {
            InstallCommand = new DelegateCommand(OnInstall);
            RefreshCommand = new DelegateCommand(OnRefresh);
            SwitchItemCmd = new DelegateCommand<SelectionChangedEventArgs>(Switch);

            LoadData();

            ItemsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            ItemsView.Filter = new Predicate<object>(o => Filter(o as ApplicationData));
        }

        private bool Filter(ApplicationData item)
        {
            return SearchText == null
                || item.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) != -1;
        }

        private void OnRefresh()
        {
            DataList.Clear();

            LoadData();
        }

        private void Switch(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                SelectedPackage = null;
                return;
            }

            if (e.AddedItems[0] is ApplicationData item)
            {
                SelectedPackage = item;
            }
        }

        private void OnInstall()
        {
            if (SelectedPackage != null)
            {
                DoInstall(SelectedPackage);
            }
            else
            {
                Status = Lang.ResourceManager.GetString("SelectApp");
            }
        }
        private void DoInstall(ApplicationData selected)
        {
            Status = string.Format(Lang.ResourceManager.GetString("StartedInstalling"), selected.Name + " " + selected.Version);


            Status = Lang.ResourceManager.GetString("StartInstall");
            IsEnabled = false;

            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"install {selected.Id}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };

            proc.OutputDataReceived += (o, args) =>
            {
                string line = args.Data ?? "";

                Debug.WriteLine(line);

                Application.Current.Dispatcher.Invoke(() =>
                {
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
                        Status = Lang.ResourceManager.GetString("Hash");
                    }

                    if (line.Contains("Installing"))
                    {
                        Status = Lang.ResourceManager.GetString("Installing");
                    }

                    if (line.Contains("Failed"))
                    {
                        Status = Lang.ResourceManager.GetString("InstallFail");
                    }
                });
            };

            proc.Exited += (o, args) =>
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    string text = (o as Process).ExitCode != 0
                        ? string.Format(Lang.ResourceManager.GetString("InstallFailParameter"), selected.Name)
                        : string.Format(Lang.ResourceManager.GetString("Installed"), selected.Name + " " +
                    selected.Version);

                    Status = text;

                    IsEnabled = true;
                });
            };

            proc.Start();
            proc.BeginOutputReadLine();
        }

        private async void LoadData()
        {
            IsBusy = true;

            DataList.AddRange(await GetData());

            IsBusy = false;
        }

        private Task<ObservableCollection<ApplicationData>> GetData()
        {
            return Task.Run(() =>
            {
                ObservableCollection<ApplicationData> apps = new ObservableCollection<ApplicationData>();

                ObservableCollection<string> lines = new ObservableCollection<string>();

                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "show",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    lines.Add(line);
                }

                try
                {
                    foreach (string line in lines.Skip(4))
                    {
                        ApplicationData newAppData = new ApplicationData
                        {
                            Name = line.Substring(0, 30).Trim(),
                            Id = line.Substring(29, 44).Trim(),
                            Version = line.Substring(73).Trim()
                        };

                        apps.Add(newAppData);
                    }
                }
                catch (ArgumentOutOfRangeException)
                {

                    LoadData();
                }


                return apps;
            });
        }
    }
}
