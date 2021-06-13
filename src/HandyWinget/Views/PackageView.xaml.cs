using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Downloader;
using HandyControl.Tools;
using HandyWinget.Common;
using HandyWinget.Database;
using Microsoft.EntityFrameworkCore;
using ModernWpf.Controls;
using static HandyWinget.Common.Helper;
using static HandyControl.Tools.DispatcherHelper;
using static HandyWinget.Common.DatabaseOperation;
using HandyWinget.Control;
using System.ComponentModel;
using System.Windows.Data;
using HandyWinget.Common.Models;
using System.Linq;
using HandyControl.Controls;
using YamlDotNet.Core.Tokens;

namespace HandyWinget.Views
{
    public partial class PackageView : UserControl
    {
        private bool hasLoaded = false;
        private bool hasViewLoaded = false;
        public PackageView()
        {
            InitializeComponent();
            Loaded += PackageView_Loaded;
            initSettings();
        }

        private void initSettings()
        {
            txtUpdateDate.Text = $"Last Update: {Settings.UpdatedDate}";
            ((App) Application.Current).UpdateAccent(Settings.Accent);
        }

        private void PackageView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasViewLoaded)
            {
                hasViewLoaded = true;
                if (!File.Exists(Consts.HWGDatabasePath) || Settings.AutoRefreshInStartup)
                {
                    btnUpdate_Click(null, null);
                }
                else
                {
                    LoadDatabaseAsync();
                }

                if (Settings.IsStoreDataGridColumnWidth)
                {
                    if (Settings.DataGridColumnWidth.Count > 0)
                    {
                        for (var i = 0; i < dataGrid.Columns.Count; i++)
                        {
                            dataGrid.Columns[i].Width = Settings.DataGridColumnWidth[i];
                        }
                    }

                    if (Settings.DataGridInstalledColumnWidth.Count > 0)
                    {
                        for (var i = 0; i < dataGridInstalled.Columns.Count; i++)
                        {
                            dataGridInstalled.Columns[i].Width = Settings.DataGridInstalledColumnWidth[i];
                        }
                    }

                    hasLoaded = true;
                }
            }
        }

        /// <summary>
        /// Group DataGrid based on Publisher
        /// </summary>
        private void SetGroupDataGrid()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);

            if (Settings.GroupByPublisher)
            {
                if (view != null)
                {
                    view.GroupDescriptions.Clear();
                    view.GroupDescriptions.Add(new PropertyGroupDescription("Publisher"));
                }
            }
            else
            {
                if (view != null)
                {
                    view.GroupDescriptions.Clear();
                }
            }
        }

        /// <summary>
        /// Download MSIX from Azure
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            bool _isConnected = ApplicationHelper.IsConnectedToInternet();
            if (_isConnected)
            {
                txtStatus.Text = "Downloading Database...";
                txtPrgStatus.Text = string.Empty;
                prgMSIX.Value = 0;
                prgMSIX.Visibility = Visibility.Visible;
                prgMSIX.IsIndeterminate = false;
                var downloader = new DownloadService();
                downloader.DownloadProgressChanged += Downloader_DownloadProgressChanged;
                downloader.DownloadFileCompleted += Downloader_DownloadFileCompleted;
                await downloader.DownloadFileTaskAsync(Consts.MSIXSourceUrl, new DirectoryInfo(Consts.MSIXPath));
            }
            else
            {
                CreateInfoBar("Network UnAvailable", "Unable to connect to the Internet", panel, Severity.Error);
            }
        }

        /// <summary>
        /// Extract MSIX into indexV4.db
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var downloadInfo = e.UserState as DownloadPackage;
            if (downloadInfo != null && downloadInfo.FileName != null)
            {
                RunOnMainThread(() => {
                    txtStatus.Text = "Extracting...";
                    prgMSIX.IsIndeterminate = true;
                    ZipFile.ExtractToDirectory(downloadInfo.FileName, Consts.MSIXPath, true);
                });
               await Task.Run(() =>
                {
                    GenerateDatabaseAsync();

                }).ContinueWith( x =>
                {
                    RunOnMainThread(async() =>
                    {
                        prgMSIX.IsIndeterminate = false;
                        prgMSIX.Visibility = Visibility.Collapsed;
                        Settings.UpdatedDate = DateTime.Now;
                        txtUpdateDate.Text = $"Last Update: {DateTime.Now}";
                    });
                    LoadDatabaseAsync();
                });
            }
        }

        private void Downloader_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var progress = (int) e.ProgressPercentage;
            RunOnMainThread(() =>
            {
                prgMSIX.Value = progress;
                txtPrgStatus.Text = $"Downloading {BytesToMegabytes(e.ReceivedBytesSize)} MB of {BytesToMegabytes(e.TotalBytesToReceive)} MB  -  {progress}%";
            });
        }

        /// <summary>
        /// Load Packages to DataGrid and Identify Installed packages
        /// </summary>
        private async void LoadDatabaseAsync()
        {
            dataGrid.ItemsSource = await GetAllPackageAsync();
            SetGroupDataGrid();

            IdentifyPackages();
        }

        #region Identify Installed Packages

        /// <summary>
        /// Identify Installed Packages
        /// </summary>
        private async void IdentifyPackages()
        {
            if (Settings.IdentifyInstalledPackage)
            {
                if (!IsOsSupported())
                {
                    CreateInfoBar("OS Not Supported", "Your operating system does not support this feature", panelInstalled, Severity.Error);
                }
                else
                {
                    prgInstalled.Visibility = Visibility.Visible;
                    var progressIndicator = new Progress<int>(ReportProgress);
                    await Task.Run(() =>
                    {
                        LoadInstalledListAsync(progressIndicator);
                    });
                }
            }
            else
            {
                CreateInfoBarWithAction("Note", "You have disabled package identification in settings, go to Settings and enable it (To be effective, you must restart HandyWinget). Note that activating this feature will reduce the loading speed.", panelInstalled, Severity.Warning, "Settings", () =>
                {
                    MainWindow.Instance.navView.SelectedItem = MainWindow.Instance.navView.MenuItems[0] as NavigationViewItem;
                });
            }
        }

        void ReportProgress(int value)
        {
            prgInstalled.Value = value;
        }

        /// <summary>
        /// Load Installed Packages into Datagrid 
        /// </summary>
        /// <param name="progress"></param>
        private async void LoadInstalledListAsync(IProgress<int> progress)
        {
            if (!IsWingetInstalled())
            {
                RunOnMainThread(() =>
                {
                    CreateInfoBarWithAction("Winget-Cli", "We need Winget-cli version 1.0 or higher to identify packages, Please download and install it first then restart HandyWinget.", panelInstalled, Severity.Error, "Download", () =>
                    {
                        StartProcess(Consts.WingetRepository);
                    });
                });

                return;
            }

            var installedData = new ThreadSafeObservableCollection<HWGInstalledPackageModel>();
            var lines = GetInstalledScript();

            if (lines == null)
            {
                RunOnMainThread(() =>
                {
                    CreateInfoBarWithAction("Update Winget-Cli", "your Winget-cli is not supported please Update your winget-cli to version 1.0 or higher.", panelInstalled, Severity.Error, "Update", () =>
                    {
                        StartProcess(Consts.WingetRepository);
                    });
                });
                return;
            }
            var query = await GetAllPackageAsync();
            var queryCount = query.Count();
            int currentItemIndex = 0;
            foreach (var packageItem in query)
            {
                currentItemIndex += 1;
                progress.Report((currentItemIndex * 100 / queryCount));
                foreach (var installedItem in lines)
                {
                    var item = ParseInstallScriptLine(installedItem, packageItem.PackageId);
                    if (item.packageId != null && item.version != null)
                    {
                        if (packageItem.PackageId.Equals(item.packageId, StringComparison.OrdinalIgnoreCase))
                        {
                            installedData.Add(new HWGInstalledPackageModel
                            {
                                Name = packageItem.Name,
                                PackageId = packageItem.PackageId,
                                Publisher = packageItem.Publisher,
                                ProductCode = packageItem.ProductCode,
                                YamlUri = packageItem.YamlUri,
                                Version = item.version,
                                AvailableVersion = item.availableVersion
                            });
                            break;
                        }
                    }
                }
            }

            RunOnMainThread(() =>
            {
                prgInstalled.Visibility = Visibility.Collapsed;
                dataGridInstalled.ItemsSource = installedData;
            });
        }
        #endregion

        private async void GetManifestAsync()
        {
            using var db = new HWGContext();
            using var client = new HttpClient();

            var list = await db.ManifestTable.ToListAsync();


            //var link = $"{Consts.AzureBaseUrl}{item.YamlName}";
            //var responseString = await client.GetStringAsync(link).ConfigureAwait(false);
        }

        #region Filter DataGrid
        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(autoBox.Text))
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                view.Filter = new Predicate<object>(filterPackages);
            }
        }
        private bool filterPackages(object item)
        {
            var search = item as HWGPackageModel;
            if (search.PackageId.Contains(autoBox.Text, StringComparison.OrdinalIgnoreCase) ||
                search.Name.Contains(autoBox.Text, StringComparison.OrdinalIgnoreCase) ||
                search.Publisher.Contains(autoBox.Text, StringComparison.OrdinalIgnoreCase)) { 

                return true;
            }
            return false;
        }

        #endregion

        #region DataGrid Layout Updated
        private void dataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            if (!hasLoaded)
                return;
            if (Settings.IsStoreDataGridColumnWidth)
            {
                for (int i = Settings.DataGridColumnWidth.Count; i < dataGrid.Columns.Count; i++)
                {
                    Settings.DataGridColumnWidth.Add(default);
                }

                for (int index = 0; index < dataGrid.Columns.Count; index++)
                {
                    if (dataGrid.Columns == null)
                        return;
                    Settings.DataGridColumnWidth[index] = new DataGridLength(dataGrid.Columns[index].ActualWidth);
                }
            }
        }

        private void dataGridInstalled_LayoutUpdated(object sender, EventArgs e)
        {
            if (!hasLoaded)
                return;
            if (Settings.IsStoreDataGridColumnWidth)
            {
                for (int i = Settings.DataGridInstalledColumnWidth.Count; i < dataGridInstalled.Columns.Count; i++)
                {
                    Settings.DataGridInstalledColumnWidth.Add(default);
                }

                for (int index = 0; index < dataGridInstalled.Columns.Count; index++)
                {
                    if (dataGridInstalled.Columns == null)
                        return;
                    Settings.DataGridInstalledColumnWidth[index] = new DataGridLength(dataGridInstalled.Columns[index].ActualWidth);
                }
            }
        }

        #endregion

        #region ContextMenu
        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem button)
            {
                ContextMenuActions(button.Tag.ToString());
            }
        }
        private void ContextMenuActions(string tag)
        {
            
        }
        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {

        }

        #endregion

    }
}
