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
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using System.Windows.Input;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace HandyWinget.Views
{
    public partial class PackageView : UserControl
    {
        private bool hasLoaded = false;
        private bool hasViewLoaded = false;

        public PackageView()
        {
            InitializeComponent();
            DataContext = this;
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
                if (!IsWingetInstalled())
                {
                    CreateInfoBarWithAction("Winget-Cli", "We need Winget-cli version 1.0 or higher to identify packages, Please download and install it first then restart HandyWinget.", panelInstalled, Severity.Error, "Download", () =>
                    {
                        StartProcess(Consts.WingetRepository);
                    });
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

        #region Filter DataGrid
        ICollectionView view;
        ICollectionView viewInstalled;

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(autoBox.Text))
            {
                view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if (view == null)
                    return;
                view.Filter = new Predicate<object>(filterPackages);
            }
            view?.Refresh();
        }
        private void autoBoxInstalled_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(autoBoxInstalled.Text))
            {
                viewInstalled = CollectionViewSource.GetDefaultView(dataGridInstalled.ItemsSource);
                if (viewInstalled == null)
                    return;
                viewInstalled.Filter = new Predicate<object>(filterInstalledPackages);
            }
            viewInstalled?.Refresh();
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

        private bool filterInstalledPackages(object item)
        {
            var search = item as HWGInstalledPackageModel;
            if (search.PackageId.Contains(autoBoxInstalled.Text, StringComparison.OrdinalIgnoreCase) ||
                search.Name.Contains(autoBoxInstalled.Text, StringComparison.OrdinalIgnoreCase) ||
                search.Publisher.Contains(autoBoxInstalled.Text, StringComparison.OrdinalIgnoreCase))
            {

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
        private void DataGridContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var selectedRows = dataGrid.SelectedItems.Count;

            if (selectedRows > 1)
            {
                mnuCopyScript.IsEnabled = false;
                mnuSendToCmd.IsEnabled = false;
            }
            else
            {
                mnuCopyScript.IsEnabled = true;
                mnuSendToCmd.IsEnabled = true;
            }
        }

        private void DataGridContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem button)
            {
                DataGridContextMenuActions(button.Tag.ToString());
            }
        }

        private void DataGridContextMenuActions(string tag)
        {
            var selectedRows = dataGrid.SelectedItems.Count;
            var item = dataGrid.SelectedItem as HWGPackageModel;

            string text = $"winget install {item.PackageId} -v {item.PackageVersion.Version}";
            switch (tag)
            {
                case "SendToPow":
                    if (selectedRows > 1)
                    {
                        var script = CreatePowerShellScript(false);
                        Process.Start("powershell.exe", script);
                    }
                    else if (selectedRows == 1)
                    {
                        Process.Start("powershell.exe", text);
                    }
                    break;
                case "SendToCmd":
                    if (selectedRows == 1)
                    {
                        Interaction.Shell(text, AppWinStyle.NormalFocus);
                    }
                    break;
                case "Copy":
                    if (selectedRows == 1)
                    {
                        Clipboard.SetText(text);
                    }
                    break;
                case "Export":
                    ExportPowerShellScript();
                    break;
            }

        }

        private void DataGridInstalledContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var selectedRows = dataGridInstalled.SelectedItems.Count;

            if (selectedRows > 1)
            {
                mnuUpgrade.IsEnabled = false;
                mnuUninstall.IsEnabled = false;
                mnuInstalledCopyScript.IsEnabled = false;
            }
            else
            {
                mnuUpgrade.IsEnabled = true;
                mnuUninstall.IsEnabled = true;
                mnuInstalledCopyScript.IsEnabled = true;
            }

            if (!IsWingetInstalled())
            {
                mnuUpgrade.IsEnabled = false;
                mnuUninstall.IsEnabled = false;
            }
        }
        private void DataGridInstalledContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem button)
            {
                DataGridInstalledContextMenuActions(button.Tag.ToString());
            }
        }
        private async void DataGridInstalledContextMenuActions(string tag)
        {
            var selectedRows = dataGridInstalled.SelectedItems.Count;
            var item = dataGridInstalled.SelectedItem as HWGInstalledPackageModel;

            string text = $"winget install {item.PackageId} -v {item.Version}";
            switch (tag)
            {
                case "Copy":
                    if (selectedRows == 1)
                    {
                        Clipboard.SetText(text);
                    }
                    break;
                case "Export":
                    ExportPowerShellScript(true);
                    break;
                case "Upgrade":
                    if (selectedRows == 1 && !string.IsNullOrEmpty(item.AvailableVersion))
                    {
                        var result = false;
                        mnuUpgrade.IsEnabled = false;
                        await Task.Run(() => {
                             result = UpgradePackage(item.PackageId);
                        });
                        if (result)
                        {
                            CreateInfoBar("Upgrade", $"Your Selected Package ({item.PackageId}) Successfully Upgraded", panelInstalled, Severity.Success);
                        }
                        else
                        {
                            CreateInfoBar("Upgrade", $"We Cant Upgrade Your Selected Package ({item.PackageId})", panelInstalled, Severity.Error);
                        }
                    }
                    else
                    {
                        CreateInfoBar("Upgrade", $"Your Selected Package ({item.PackageId}) does not have any Available Version", panelInstalled, Severity.Error);
                    }
                    mnuUpgrade.IsEnabled = true;

                    break;
                case "Uninstall":
                    if (selectedRows == 1 && !string.IsNullOrEmpty(item.ProductCode))
                    {
                        var result = false;
                        mnuUninstall.IsEnabled = false;
                        await Task.Run(() => {
                            result = UninstallPackage(item.ProductCode);
                        });
                        if (result)
                        {
                            CreateInfoBar("Uninstall", $"Your Selected Package ({item.PackageId}) Successfully Uninstalled", panelInstalled, Severity.Success);
                        }
                        else
                        {
                            CreateInfoBar("Uninstall", $"We Cant Uninstall Your Selected Package ({item.PackageId})", panelInstalled, Severity.Error);
                        }
                    }
                    else
                    {
                        CreateInfoBar("Uninstall", $"Your Selected Package ({item.PackageId}) does not have a productCode", panelInstalled, Severity.Error);
                    }
                    mnuUninstall.IsEnabled = true;

                    break;
            }
        }

        public async void ExportPowerShellScript(bool isInstalled = false)
        {
            var itemsCount = isInstalled ? dataGridInstalled.SelectedItems.Count : dataGrid.SelectedItems.Count;
            if (itemsCount > 0)
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
                    if (isInstalled)
                    {
                        await File.WriteAllTextAsync(dialog.FileName, CreatePowerShellScript(true, true));
                    }
                    else
                    {
                        await File.WriteAllTextAsync(dialog.FileName, CreatePowerShellScript(true));
                    }
                }
            }
        }

        private string CreatePowerShellScript(bool isExportScript, bool isInstalled = false)
        {
            StringBuilder builder = new StringBuilder();
            if (isExportScript)
            {
                builder.Append(Helper.PowerShellScript);
            }

            if (isInstalled)
            {
                foreach (var item in dataGridInstalled.SelectedItems)
                {
                    builder.Append($"winget install {((HWGInstalledPackageModel) item).PackageId} -v {((HWGInstalledPackageModel) item).Version} -e ; ");
                }
            }
            else
            {
                foreach (var item in dataGrid.SelectedItems)
                {
                    builder.Append($"winget install {((HWGPackageModel) item).PackageId} -v {((HWGPackageModel) item).PackageVersion.Version} -e ; ");
                }
            }

            builder.Remove(builder.ToString().LastIndexOf(";"), 1);
            if (isExportScript)
            {
                builder.AppendLine("}");
            }

            return builder.ToString().TrimEnd();
        }

        #endregion

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.P)
            {
                DataGridContextMenuActions("SendToPow");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.W)
            {
                DataGridContextMenuActions("SendToCmd");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.C)
            {
                DataGridContextMenuActions("Copy");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.U)
            {
                DataGridContextMenuActions("Uninstall");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.X)
            {
                DataGridContextMenuActions("Export");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.U)
            {
                DataGridInstalledContextMenuActions("Upgrade");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.I)
            {
                DataGridInstalledContextMenuActions("Copy");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.E)
            {
                DataGridInstalledContextMenuActions("Export");
            }
        }

        private async void GetManifestAsync(bool isInstalled = false)
        {
            string link = string.Empty;
            var installedSelectedItem = dataGridInstalled.SelectedItem as HWGInstalledPackageModel;
            var selectedItem = dataGrid.SelectedItem as HWGPackageModel;

            if (isInstalled && installedSelectedItem != null)
            {
                link = $"{Consts.AzureBaseUrl}{installedSelectedItem.YamlUri}";
            }
            else if (!isInstalled && selectedItem != null)
            {
                link = $"{Consts.AzureBaseUrl}{selectedItem.YamlUri}";
            }
            else
            {
                return;
            }

            using var client = new HttpClient();
            var responseString = await client.GetStringAsync(link);

            if (!string.IsNullOrEmpty(responseString))
            {
                var deserializer = new DeserializerBuilder()
                                                       .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                                       .IgnoreUnmatchedProperties()
                                                       .Build();
                var result = deserializer.Deserialize<ManifestDetailModel>(responseString);
                if (result != null)
                {
                    // parse
                    Debug.WriteLine(result.PackageIdentifier);
                    Debug.WriteLine(result.PackageName);
                    Debug.WriteLine(result.PackageVersion);
                    Debug.WriteLine(result.Publisher);
                    Debug.WriteLine(result.ShortDescription);
                }
            }
        }
        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tab.SelectedIndex == 1)
            {
                GetManifestAsync(true);
            }
            else
            {
                GetManifestAsync();
            }
            tab.SelectedIndex = 2;
            e.Handled = true;
        }
        private void Grid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            //// Have to do this in the unusual case where the border of the cell gets selected.
            //// and causes a crash 'EditItem is not allowed'
            e.Cancel = true;
        }
    }
}
