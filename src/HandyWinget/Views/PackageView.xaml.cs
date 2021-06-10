using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
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
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HandyWinget.Views
{
    public partial class PackageView : UserControl
    {
        public PackageView()
        {
            InitializeComponent();
        }

        private void appBarRefresh_Click(object sender, RoutedEventArgs e)
        {
            //DownloadManifests(true);
        }

        private void appBarInstall_Click(object sender, RoutedEventArgs e)
        {
            //InstallPackage();
        }

        private void appBarIsInstalled_Checked(object sender, RoutedEventArgs e)
        {
            //FilterInstalledApps(appBarIsInstalled.IsChecked.Value);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                GenerateDatabaseAsync();

            }).ContinueWith(x =>
            {
                DownloadManifests();
            });
        }

        private async void downloadSource()
        {
            var downloader = new DownloadService();
            downloader.DownloadProgressChanged += Downloader_DownloadProgressChanged;
            downloader.DownloadFileCompleted += Downloader_DownloadFileCompleted;
            await downloader.DownloadFileTaskAsync(Consts.MSIXSourceUrl, new DirectoryInfo(Consts.MSIXPath));
        }

        private void Downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var downloadInfo = e.UserState as DownloadPackage;
            if (downloadInfo != null && downloadInfo.FileName != null)
            {
                DispatcherHelper.RunOnMainThread(() => {
                    ZipFile.ExtractToDirectory(downloadInfo.FileName, Consts.MSIXPath, true);
                });
                GenerateDatabaseAsync();
            }
        }

        private void Downloader_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            
        }

        private async void GenerateDatabaseAsync()
        {
            using var msixDB = new MSIXContext();

            var mydb = new HWGContext();

            await mydb.Database.EnsureDeletedAsync();
            await mydb.Database.EnsureCreatedAsync();

            using var db = msixDB.CreateLinqToDbConnection();

            var pathCte = db.GetCte<PathPartCte>(cte => (
                    from pathPart in msixDB.PathPartsMSIXTable
                    select new PathPartCte
                    {
                        rowid = pathPart.rowid,
                        parent = pathPart.parent,
                        path = pathPart.pathpart
                    }
                )
                .Concat(
                    from pathPart in msixDB.PathPartsMSIXTable
                    from child in cte.Where(child => child.parent == pathPart.rowid)
                    select new PathPartCte
                    {
                        rowid = child.rowid,
                        parent = pathPart.parent,
                        path = pathPart.pathpart + "\\" + child.path
                    }
                )
            );

            var query =
                from item in msixDB.IdsMSIXTable
                from manifest in msixDB.Set<ManifestMSIXTable>().Where(e => e.id == item.rowid)

                from yml in msixDB.PathPartsMSIXTable.Where(e => e.rowid == manifest.pathpart)
                from pathPartVersion in msixDB.PathPartsMSIXTable.Where(e => e.rowid == yml.parent)
                from pathPartAppName in msixDB.PathPartsMSIXTable.Where(e => e.rowid == pathPartVersion.parent)
                from pathPartPublisher in msixDB.PathPartsMSIXTable.Where(e => e.rowid == pathPartAppName.parent)
                from pathPart in pathCte.Where(e => e.rowid == pathPartPublisher.parent && e.parent == null)
                from productMap in msixDB.ProductCodesMapMSIXTable.Where(e => e.manifest == manifest.rowid).DefaultIfEmpty()
                from prdCode in msixDB.ProductCodesMSIXTable.Where(e => e.rowid == productMap.productcode).DefaultIfEmpty()
                from version in msixDB.VersionsMSIXTable.Where(e => e.rowid == manifest.version)
                select new ManifestTable
                {
                    PackageId = item.id,
                    ProductCode = prdCode.productcode,
                    YamlName = $@"{pathPart.path}\{pathPartPublisher.pathpart}\{pathPartAppName.pathpart}\{pathPartVersion.pathpart}\{yml.pathpart}",
                    Version = version.version
                };

            var data = await query.ToArrayAsyncLinqToDB();
            mydb.AddRange(data);
            await mydb.SaveChangesAsync();
        }
        private readonly HttpClient client = new HttpClient();

        private async void DownloadManifests()
        {
            using (var db = new HWGContext())
            {
               // var list = await db.ManifestTable.ToListAsync();
                //foreach (var item in list)
                //{
                //    var responseString = await client.GetStringAsync($"https://winget.azureedge.net/cache/manifests/{item.YamlName}");
                //    Debug.WriteLine(responseString);

                //}
            }

        }
    }
}
