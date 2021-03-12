using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyWinget.Assets;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Serialization;

namespace HandyWinget.Views
{
    /// <summary>
    /// Interaction logic for CreatePackage.xaml
    /// </summary>
    public partial class CreatePackage : UserControl
    {
        public CreatePackage()
        {
            InitializeComponent();

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
            txtMoniker.Text = string.Empty;
            txtTags.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtHomePage.Text = string.Empty;
            txtLicense.Text = string.Empty;
            txtLicenseUrl.Text = string.Empty;
            txtUrl.Text = string.Empty;
            txtHash.Text = string.Empty;
            prgStatus.Value = 0;
            tagContainer.Items.Clear();
        }

        public void GenerateScript(GenerateScriptMode mode)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtAppName.Text) && !string.IsNullOrEmpty(txtPublisher.Text) && !string.IsNullOrEmpty(txtId.Text) && !string.IsNullOrEmpty(txtVersion.Text)
                                               && !string.IsNullOrEmpty(txtLicense.Text) && !string.IsNullOrEmpty(txtUrl.Text) && txtUrl.Text.IsUrl())
                {

                    var tags = string.Join(",", tagContainer.Items.Cast<Tag>().Select(p => p.Content));


                    var ext = System.IO.Path.GetExtension(txtUrl.Text)?.Replace(".", "").Trim();
                    if (ext != null && ext.ToLower().Equals("msixbundle"))
                    {
                        ext = "Msix";
                    }

                    var builder = new YamlPackageModel
                    {
                        Id = txtId.Text,
                        Version = txtVersion.Text,
                        Name = txtAppName.Text,
                        Publisher = txtPublisher.Text,
                        License = txtLicense.Text,
                        LicenseUrl = txtLicenseUrl.Text,
                        AppMoniker = txtMoniker.Text,
                        Tags = tags,
                        Description = txtDescription.Text,
                        Homepage = txtHomePage.Text,
                        Installers = new List<Installer>
                    {
                        new()
                        {
                            Arch = (cmbArchitecture.SelectedItem as ComboBoxItem).Content.ToString(),
                            Url = txtUrl.Text,
                            Sha256 = txtHash.Text
                        }
                    },
                        InstallerType = ext,
                        Switches = ext != null && ext.ToLower().Equals("exe")
                            ? new Switches
                            {
                                Silent = "/S",
                                SilentWithProgress = "/S"
                            }
                            : new Switches()
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
                            dialog.FileName = $"{txtVersion.Text}.yaml";
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

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtTags.Text))
            {
                Growl.WarningGlobal("Please Enter Content");
                return;
            }

            tagContainer.Items.Add(new Tag
            {
                Content = txtTags.Text,
                ShowCloseButton = true
            });
            txtTags.Text = string.Empty;
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
                txtHash.Text = CryptographyHelper.GenerateSHA256ForFile(fileName);
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
                txtHash.Text = CryptographyHelper.GenerateSHA256ForFile(dialog.FileName);
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
    }
}
