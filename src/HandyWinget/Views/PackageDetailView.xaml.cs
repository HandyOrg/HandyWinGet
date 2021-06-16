using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
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
        string downloadedInstallerPath = string.Empty;
        string yamlLink = string.Empty;
        bool hasLoaded = false;
        DownloadService downloaderService;
        Process wingetProcess;
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
            stackComboBox.Visibility = Visibility.Collapsed;
            toogleDownload.Visibility = Visibility.Collapsed;
            progress.Visibility = Visibility.Collapsed;
            progressLoaded.Visibility = Visibility.Collapsed;
            progressLoaded.IsIndeterminate = false;
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
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

        private void toogleDownload_Checked(object sender, RoutedEventArgs e)
        {
            SetToogleDownloadContent();
            if (toogleDownload.IsChecked.Value)
            {
                if (Helper.Settings.InstallMode == InstallMode.Internal)
                {
                    DownloadPackageWithInternalDownloader();
                }
                else
                {
                    DownloadPackageWithWinGet();
                }
            }
            else
            {
                downloaderService?.CancelAsync();
            }
        }
        private void DownloadPackageWithWinGet()
        {
            if (txtId.Text != null)
            {
                toogleDownload.IsChecked = false;
                toogleDownload.IsEnabled = false;
                txtStatus.Text = $"Preparing to download {txtId.Text}";
                progress.IsIndeterminate = true;
                progress.ShowError = false;
                progress.Value = 0;
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                startInfo.FileName = @"winget";
                startInfo.Arguments = $"install {txtId.Text} -v {txtVersion.Text}";

                wingetProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                wingetProcess.OutputDataReceived += (o, args) =>
                {
                    var line = args.Data ?? "";

                    DispatcherHelper.RunOnMainThread(() =>
                    {
                        if (line.Contains("Download"))
                        {
                            txtStatus.Text = $"Downloading {txtId.Text} - {txtVersion.Text}...";
                        }

                        if (line.Contains("hash"))
                        {
                            txtStatus.Text = $"Validated hash for {txtId.Text} - {txtVersion.Text}";
                        }

                        if (line.Contains("Installing"))
                        {
                            txtStatus.Text = $"Installing {txtId.Text} - {txtVersion.Text}";
                        }

                        if (line.Contains("Failed"))
                        {
                            txtStatus.Text = $"Installation of {txtId.Text} - {txtVersion.Text} failed";
                            progress.ShowError = true;
                            toogleDownload.IsEnabled = true;
                        }

                        if (line.Contains("Please update the client"))
                        {
                            txtStatus.Text = $"Installation of {txtId.Text} - {txtVersion.Text} failed, Please update the winget-cli client.";
                            progress.ShowError = true;
                            toogleDownload.IsEnabled = true;
                            Helper.CreateInfoBar("Error", "Please update the winget-cli client.", panel, Severity.Error);
                        }
                    });
                };
                wingetProcess.Exited += (o, _) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var installFailed = (o as Process).ExitCode != 0;
                        if (installFailed)
                        {
                            txtStatus.Text = $"Installation of {txtId.Text} - {txtVersion.Text} failed";
                            toogleDownload.IsEnabled = true;
                            progress.ShowError = true;
                        }
                        else
                        {
                            txtStatus.Text = $"{txtId.Text} - {txtVersion.Text} Installed.";
                        }
                        progress.ShowError = false;
                        progress.IsIndeterminate = false;
                        toogleDownload.IsEnabled = true;
                    });
                };

                wingetProcess.Start();
                wingetProcess.BeginOutputReadLine();
            }
        }
        private async void DownloadPackageWithInternalDownloader()
        {
            try
            {
                var isConnected = ApplicationHelper.IsConnectedToInternet();

                if (isConnected)
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
                            downloadedInstallerPath = $@"{Consts.TempPath}\{txtId.Text}-{txtVersion.Text}-{(cmbArchitectures.SelectedItem as Installer)?.Architecture}{Helper.GetExtension(url)}".Trim();
                            if (!File.Exists(downloadedInstallerPath))
                            {
                                downloaderService = new DownloadService();
                                downloaderService.DownloadProgressChanged += DownloaderService_DownloadProgressChanged;
                                downloaderService.DownloadFileCompleted += DownloaderService_DownloadFileCompleted;
                                await downloaderService.DownloadFileTaskAsync(url, downloadedInstallerPath);
                            }
                            else
                            {
                                txtStatus.Text = $"{txtId.Text} Already downloaded, We are running it now!";
                                Helper.StartProcess(downloadedInstallerPath);
                                toogleDownload.IsChecked = false;
                            }
                        }
                    }
                }
                else
                {
                    Helper.CreateInfoBar("Network UnAvailable", "Unable to connect to the Internet", panel, Severity.Error);
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
                Helper.StartProcess(downloadedInstallerPath);
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
