using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        }

        private void HideControls()
        {
            stackComboBox.Visibility = System.Windows.Visibility.Collapsed;
            toogleDownload.Visibility = System.Windows.Visibility.Collapsed;
            progress.Visibility = System.Windows.Visibility.Collapsed;
            txtProgress.Visibility = System.Windows.Visibility.Collapsed;
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
    }
}
