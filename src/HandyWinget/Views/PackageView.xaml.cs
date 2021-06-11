using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyControl.Tools.Extension;
using HandyWinget.Common;
using HandyWinget.Database;
using HandyWinget.Database.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ModernWpf.Controls;
using static HandyWinget.Common.Helper;
using static HandyControl.Tools.DispatcherHelper;
using HandyWinget.Control;
using Microsoft.VisualBasic;

namespace HandyWinget.Views
{
    public partial class PackageView : UserControl
    {
        private bool hasLoaded = false;

        public PackageView()
        {
            InitializeComponent();
            Loaded += PackageView_Loaded;
            initSettings();
        }

        private void initSettings()
        {
            txtUpdateDate.Text = $"Last Update: {Settings.UpdatedDate}";
        }

        private void PackageView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Consts.HWGDatabasePath) || Settings.AutoRefreshInStartup)
            {
                btnUpdate_Click(null, null);
            }
            else
            {
                //LoadDatabaseAsync();
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

                hasLoaded = true;
            }
        }

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

        private void Downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var downloadInfo = e.UserState as DownloadPackage;
            if (downloadInfo != null && downloadInfo.FileName != null)
            {
                RunOnMainThread(() => {
                    txtStatus.Text = "Extracting...";
                    prgMSIX.IsIndeterminate = true;
                    ZipFile.ExtractToDirectory(downloadInfo.FileName, Consts.MSIXPath, true);
                });
                Task.Run(() =>
                {
                    DatabaseOperation.GenerateDatabaseAsync();

                }).ContinueWith( x =>
                {
                    RunOnMainThread(async() =>
                    {
                        prgMSIX.IsIndeterminate = false;
                        prgMSIX.Visibility = Visibility.Collapsed;
                        Settings.UpdatedDate = DateTime.Now;
                        txtUpdateDate.Text = $"Last Update: {DateTime.Now}";
                        LoadDatabaseAsync();
                    });
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

        private async void LoadDatabaseAsync()
        {
            using var db = new HWGContext();
            dataGrid.ItemsSource = await db.ManifestTable.ToListAsync();
        }

        private async void GetManifestAsync()
        {
            using var db = new HWGContext();
            using var client = new HttpClient();

            var list = await db.ManifestTable.ToListAsync();


            //var link = $"{Consts.AzureBaseUrl}{item.YamlName}";
            //var responseString = await client.GetStringAsync(link).ConfigureAwait(false);
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {

        }

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
    }
}
