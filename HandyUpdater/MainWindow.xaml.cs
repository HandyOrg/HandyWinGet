using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace HandyUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        WebClient client = new WebClient();

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

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            btnDownload.Content = "Downloading...";
            btnDownload.IsEnabled = false;

            try
            {
                client = new WebClient();
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                client.DownloadFileAsync(new Uri(App.Argument.Url), tempRarFile);
            }

            catch (Exception) { }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            prgStatus.Value = int.Parse(Math.Truncate(percentage).ToString());
            txtStatus.Text = prgStatus.Value + "%  -  Downloaded " + ConvertBytesToMegabytes(e.BytesReceived).ToString("N2") + " of " + ConvertBytesToMegabytes(e.TotalBytesToReceive).ToString("N2");
        }
        double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            btnDownload.Content = "Downloaded";
            File.Delete(App.Argument?.ExeLocation);
            UnRar();
            File.Delete(tempRarFile);
            Process.Start(App.Argument?.ExeLocation);
            Environment.Exit(0);
        }

        private void UnRar()
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = "UnRAR.exe";
            p.StartInfo.Arguments = string.Format(@"x -s ""{0}"" *.*", tempRarFile);
            p.Start();
            p.WaitForExit();
        }

    }
}
