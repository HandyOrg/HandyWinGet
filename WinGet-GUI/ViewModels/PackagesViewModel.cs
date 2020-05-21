using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using WinGet_GUI.Models;

namespace WinGet_GUI.ViewModels
{
    public class PackagesViewModel : BindableBase
    {
        private ObservableCollection<ApplicationData> _dataList = new ObservableCollection<ApplicationData>();
        public ObservableCollection<ApplicationData> DataList
        {
            get => _dataList;
            set => SetProperty(ref _dataList, value);
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

        public DelegateCommand InstallCommand { get; set; }
        public DelegateCommand RefreshCommand { get; set; }

        public ICollectionView ItemsView => CollectionViewSource.GetDefaultView(DataList);

        public PackagesViewModel()
        {
            InstallCommand = new DelegateCommand(OnInstall);
            RefreshCommand = new DelegateCommand(OnRefresh);
            LoadData();

            DataList.Clear();
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
            LoadData();
        }

        private void OnInstall()
        {
        }

        private async void LoadData()
        {
            DataList.Clear();
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

                return apps;
            });
        }
    }
}
