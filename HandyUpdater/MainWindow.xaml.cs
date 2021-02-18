using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Downloader;
namespace HandyUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        string tempRarFile = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            lblCurrent.Content = "Current Version: " + App.Argument?.CurrentVersion;
            lblNew.Content = "New Version: " + App.Argument?.NewVersion;
            btnDownload.Content = "Download New Update";
            prgStatus.Value = 0;
            tempRarFile = App.Argument?.Location + @"\temp.rar";
            if (File.Exists(tempRarFile))
            {
                File.Delete(tempRarFile);
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            btnDownload.Content = "Downloading...";
            btnDownload.IsEnabled = false;

            DownloadService downloadService = new DownloadService();
            downloadService.DownloadProgressChanged += DownloadService_DownloadProgressChanged;
            downloadService.DownloadFileCompleted += DownloadService_DownloadFileCompleted;
            await downloadService.DownloadFileAsync(App.Argument.Url, tempRarFile);
        }

        private void DownloadService_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            btnDownload.Content = "Downloaded";
            File.Delete(App.Argument?.ExeLocation);
            UnRar();
            File.Delete(tempRarFile);
            Process.Start(App.Argument?.ExeLocation);
            Environment.Exit(0);
        }

        private void DownloadService_DownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            prgStatus.Value = e.ProgressPercentage;
            txtStatus.Text = $"Downloaded {ConvertBytesToMegabytes(e.ReceivedBytesSize):N2} MB of {ConvertBytesToMegabytes(e.TotalBytesToReceive):N2} MB  -  {(int)e.ProgressPercentage} %";
        }

        double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
        private void UnRar()
        {
            Process p = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "UnRAR.exe",
                    Arguments = $@"x -s ""{tempRarFile}"" *.*"
                }
            };
            p.Start();
            p.WaitForExit();
        }

    }
}
