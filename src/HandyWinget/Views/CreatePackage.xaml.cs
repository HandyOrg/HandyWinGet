using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyWinget.Assets;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Serialization;

namespace HandyWinget.Views
{
    /// <summary>
    /// Interaction logic for CreatePackage.xaml
    /// </summary>
    public partial class CreatePackage : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<Installer> installers = new ObservableCollection<Installer>();

        public ObservableCollection<Installer> Installers
        {
            get { return installers; }
            set 
            { 
                installers = value;
                RaisePropertyChanged();
            }
        }

        public CreatePackage()
        {
            InitializeComponent();
            DataContext = this;
            if (Helper.IsWingetInstalled())
            {
                btnValidate.IsEnabled = true;
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            GenerateScript(GenerateScriptMode.SaveToFile);
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            GenerateScript(GenerateScriptMode.CopyToClipboard);
        }

        public void ClearInputs()
        {
            txtAppName.Text = string.Empty;
            txtPublisher.Text = string.Empty;
            txtId.Text = string.Empty;
            txtVersion.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtHomePage.Text = string.Empty;
            txtLicense.Text = string.Empty;
            txtLicenseUrl.Text = string.Empty;
            txtUrl.Text = string.Empty;
            txtHash.Text = string.Empty;
            prgStatus.Value = 0;
            Installers.Clear();
        }

        public void GenerateScript(GenerateScriptMode mode)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtAppName.Text) && !string.IsNullOrEmpty(txtPublisher.Text) && !string.IsNullOrEmpty(txtId.Text) && !string.IsNullOrEmpty(txtVersion.Text)
                                               && !string.IsNullOrEmpty(txtLicense.Text) && !string.IsNullOrEmpty(txtUrl.Text) && txtUrl.Text.IsUrl())
                {
                    var builder = new YamlPackageModel
                    {
                        PackageIdentifier = txtId.Text,
                        PackageVersion = txtVersion.Text,
                        PackageName = txtAppName.Text,
                        Publisher = txtPublisher.Text,
                        License = txtLicense.Text,
                        LicenseUrl = txtLicenseUrl.Text,
                        ShortDescription = txtDescription.Text,
                        PackageUrl = txtHomePage.Text,
                        ManifestType = "singleton",
                        ManifestVersion = "1.0.0",
                        PackageLocale = "en-US",
                        Installers = Installers.ToList()
                    };

                    var serializer = new SerializerBuilder().Build();
                    var yaml = serializer.Serialize(builder);
                    switch (mode)
                    {
                        case GenerateScriptMode.CopyToClipboard:
                            Clipboard.SetText(yaml);
                            Growl.SuccessGlobal("Script Copied to clipboard.");
                            ClearInputs();
                            break;
                        case GenerateScriptMode.SaveToFile:
                            var dialog = new SaveFileDialog();
                            dialog.Title = "Save Package";
                            dialog.FileName = $"{txtId.Text}.yaml";
                            dialog.DefaultExt = "yaml";
                            dialog.Filter = "Yaml File (*.yaml)|*.yaml";
                            if (dialog.ShowDialog() == true)
                            {
                                File.WriteAllText(dialog.FileName, yaml);
                                ClearInputs();
                            }

                            break;
                    }
                }
                else
                {
                    Growl.ErrorGlobal("Required fields must be filled");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        private async void btnGetHashWeb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtUrl.Text) && txtUrl.Text.IsUrl())
                {
                    prgStatus.IsIndeterminate = false;
                    btnGetHashWeb.IsEnabled = false;
                    btnGetHashLocal.IsEnabled = false;
                    txtHash.IsEnabled = false;
                    try
                    {
                        var downloader = new DownloadService();
                        downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                        downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                        await downloader.DownloadFileTaskAsync(txtUrl.Text, new DirectoryInfo(Consts.TempSetupPath));
                    }
                    catch (Exception ex)
                    {
                        prgStatus.IsIndeterminate = true;
                        prgStatus.ShowError = true;
                        Growl.ErrorGlobal(ex.Message);
                    }
                }
                else
                {
                    Growl.ErrorGlobal("Url field is Empty or Invalid");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() => {
                string fileName = ((DownloadPackage)e.UserState).FileName;
                prgStatus.Value = 0;
                txtHash.Text = CryptographyHelper.GenerateSHA256FromFile(fileName);
                btnGetHashWeb.IsEnabled = true;
                btnGetHashLocal.IsEnabled = true;
                txtHash.IsEnabled = true;

                File.Delete(fileName);
            });
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() => {
                prgStatus.Value = (int)e.ProgressPercentage;
            });
        }

        private void btnGetHashLocal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Open Setup File";
            dialog.Filter = "All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                txtHash.Text = CryptographyHelper.GenerateSHA256FromFile(dialog.FileName);
            }
        }

        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Open Yaml File";
            dialog.Filter = "Yaml File (*.yaml)|*.yaml";
            if (dialog.ShowDialog() == true)
            {
                string command = $"/K winget validate {dialog.FileName}";
                Process.Start("cmd.exe", command);
            }
        }

        private void txtPublisher_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtId.Text = $"{txtAppName.Text}.{txtPublisher.Text}";
        }

        private void btnAddInstaller_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUrl.Text) || !string.IsNullOrEmpty(txtHash.Text))
            {
                var arch = (cmbArchitecture.SelectedItem as ComboBoxItem).Content.ToString();
                var item = new Installer
                {
                    Architecture = arch,
                    InstallerUrl = txtUrl.Text,
                    InstallerSha256 = txtHash.Text
                };

                if (!Installers.Contains(item, new GenericCompare<Installer>(x => x.Architecture)))
                {
                    Installers.Add(item);
                }
                else
                {
                    Growl.ErrorGlobal($"{arch} Architecture already exist.");
                }
            }
            else
            {
                Growl.ErrorGlobal("Installer Url and Installer Sha256 must be filled");
            }
        }

        private void btnRemoveInstaller_Click(object sender, RoutedEventArgs e)
        {
            var item = lstInstaller.SelectedItem as Installer;
            if (item != null)
            {
                Installers.Remove(item);
            }
            else
            {
                Growl.ErrorGlobal("Please Select Installer from list");
            }
        }
    }
}
