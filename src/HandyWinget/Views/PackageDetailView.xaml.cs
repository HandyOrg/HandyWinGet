using System.Net.Http;
using System.Windows.Controls;
using HandyWinget.Common.Models;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using HandyControl.Tools;
using HandyWinget.Control;
using HandyWinget.Common;
using System.Threading.Tasks;

namespace HandyWinget.Views
{
    public partial class PackageDetailView : UserControl
    {
        bool hasLoaded = false;
        string yamlLink = string.Empty;
        public PackageDetailView(string yamlLink, bool isInstalled = false)
        {
            InitializeComponent();
            this.yamlLink = yamlLink;
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
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!hasLoaded)
            {
                GetManifestAsync();
            }
        }

        private async void GetManifestAsync()
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
                            hasLoaded = true;
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
            }
        }
    }
}
