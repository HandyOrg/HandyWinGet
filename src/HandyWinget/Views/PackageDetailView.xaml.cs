using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using Downloader;
using HandyControl.Tools;
using HandyWinget.Common;
using HandyWinget.Common.Models;
using HandyWinget.Control;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HandyWinget.Views
{
    public partial class PackageDetailView : UserControl
    {
        private string _TempSetupPath = string.Empty;
        public DownloadService downloaderService;
        bool hasLoaded = false;
        string yamlLink = string.Empty;
        List<PackageVersion> versions;
        public PackageDetailView(string yamlLink, List<PackageVersion> versions, bool isInstalled = false)
        {
            InitializeComponent();
            this.yamlLink = yamlLink;
            this.versions = versions;
            if (isInstalled)
            {
                HideControls();
            }
            SetToogleDownloadContent();
        }

        private void HideControls()
        {
            stackComboBox.Visibility = System.Windows.Visibility.Collapsed;
            toogleDownload.Visibility = System.Windows.Visibility.Collapsed;
            progress.Visibility = System.Windows.Visibility.Collapsed;
            progressLoaded.Visibility = System.Windows.Visibility.Collapsed;
            progressLoaded.IsIndeterminate = false;
        }
        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!hasLoaded)
            {
                await GetManifestAsync(yamlLink);
            }
        }

        private async Task<ManifestDetailModel> GetManifestAsync(string yamlLink)
        {
            var isConnected = ApplicationHelper.IsConnectedToInternet();
            if (isConnected)
            {
                try
                {
                    using var client = new HttpClient();
                    var responseString = await client.GetStringAsync(yamlLink);

                    if (!string.IsNullOrEmpty(responseString))
                    {
                        var deserializer = new DeserializerBuilder()
                                                               .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                                               .IgnoreUnmatchedProperties()
                                                               .Build();
                        var result = deserializer.Deserialize<ManifestDetailModel>(responseString);
                        if (result != null)
                        {
                            progressLoaded.Visibility = System.Windows.Visibility.Collapsed;
                            progressLoaded.IsIndeterminate = false;
                            txtId.Text = result.PackageIdentifier;
                            txtName.Text = result.PackageName;
                            txtPublisher.Text = result.Publisher;
                            txtVersion.Text = result.PackageVersion;
                            txtLicense.Text = result.License;
                            txtDescription.Text = result.ShortDescription;
                            cmbVersions.ItemsSource = versions;
                            hasLoaded = true;
                            toogleDownload.IsEnabled = true;
                            return result;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }
            else
            {
                HideControls();
                Helper.CreateInfoBar("Network UnAvailable", "Unable to connect to the Internet", panel, Severity.Error);
                return null;
            }
            return null;
        }

        private async void cmbVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVersions.SelectedItem != null)
            {
                var item = cmbVersions.SelectedItem as PackageVersion;
                cmbArchitectures.ItemsSource = null;
                var detail = await GetManifestAsync($"{Consts.AzureBaseUrl}{item.YamlUri}");
                if (detail != null)
                {
                    cmbArchitectures.ItemsSource = detail.Installers;
                    cmbArchitectures.SelectedIndex = 0;
                }
            }
        }

        private void toogleDownload_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetToogleDownloadContent();
            if (toogleDownload.IsChecked.Value)
            {
                if (Helper.Settings.InstallMode == InstallMode.Internal)
                {
                    DownloadPackage();
                }
            }
            else
            {
                downloaderService?.CancelAsync();
            }
        }

        private async void DownloadPackage()
        {
            try
            {
                var item = cmbArchitectures.SelectedItem as Installer;
                if (item != null)
                {
                    var url = Helper.RemoveUrlComment(item.InstallerUrl);

                    if (Helper.Settings.IsIDMEnabled)
                    {
                        toogleDownload.IsChecked = false;
                        Helper.DownloadWithIDM(url);
                    }
                    else
                    {
                        txtStatus.Text = $"Preparing to download {txtId.Text}";
                        progress.IsIndeterminate = false;
                        progress.ShowError = false;
                        progress.Value = 0;
                        _TempSetupPath = $@"{Consts.TempPath}\{txtId.Text}-{txtVersion.Text}-{(cmbArchitectures.SelectedItem as Installer)?.Architecture}{Helper.GetExtension(url)}".Trim();
                        if (!File.Exists(_TempSetupPath))
                        {
                            downloaderService = new DownloadService();
                            downloaderService.DownloadProgressChanged += DownloaderService_DownloadProgressChanged;
                            downloaderService.DownloadFileCompleted += DownloaderService_DownloadFileCompleted;
                            await downloaderService.DownloadFileTaskAsync(url, _TempSetupPath);
                        }
                        else
                        {
                            txtStatus.Text = $"{txtId.Text} Already downloaded, We are running it now!";
                            Helper.StartProcess(_TempSetupPath);
                            toogleDownload.IsChecked = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Helper.CreateInfoBar("Error", ex.Message, panel, Severity.Error);
                toogleDownload.IsChecked = false;
            }
        }

        private void DownloaderService_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    Helper.CreateInfoBar("Error", "Operation Canceled.", panel, Severity.Error);
                    progress.IsIndeterminate = true;
                    progress.ShowError = true;
                    txtStatus.Text = "Download Canceled!";
                    toogleDownload.IsChecked = false;
                });
            }
            else if (e.Error != null)
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    Helper.CreateInfoBar("Error", e.Error.Message, panel, Severity.Error);
                    progress.IsIndeterminate = true;
                    progress.ShowError = true;
                    txtStatus.Text = "Something is wrong!";
                    toogleDownload.IsChecked = false;
                });
            }
            else
            {
                Helper.StartProcess(_TempSetupPath);
                DispatcherHelper.RunOnMainThread(() =>
                {
                    txtStatus.Text = "Download Completed!";
                    toogleDownload.IsChecked = false;
                });
            }
        }

        private void DownloaderService_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() =>
            {
                progress.Value = (int) e.ProgressPercentage;
                txtStatus.Text = $"Downloading {txtId.Text}-{txtVersion.Text} - {Helper.BytesToMegabytes(e.ReceivedBytesSize)} MB of {Helper.BytesToMegabytes(e.TotalBytesToReceive)} MB";
            });
        }

        private void SetToogleDownloadContent()
        {
            if (Helper.Settings.InstallMode == InstallMode.Internal && !toogleDownload.IsChecked.Value)
            {
                toogleDownload.Content = "Download";
            }
            else if (Helper.Settings.InstallMode == InstallMode.Internal && toogleDownload.IsChecked.Value || Helper.Settings.InstallMode == InstallMode.Wingetcli && toogleDownload.IsChecked.Value)
            {
                toogleDownload.Content = "Cancel";
            }
            else if (Helper.Settings.InstallMode == InstallMode.Wingetcli && !toogleDownload.IsChecked.Value)
            {
                toogleDownload.Content = @"Download/Install";
            }
        }
    }
}
