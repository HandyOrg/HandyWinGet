using System.Windows;
using System.Windows.Controls;

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
    }
}
