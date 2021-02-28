using Downloader;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using HandyWinget.Assets;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using ModernWpf.Controls;
using Newtonsoft.Json;
using nucs.JsonSettings;
using nucs.JsonSettings.Autosave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using YamlDotNet.Serialization;

namespace HandyWinget.Views
{
    public partial class Packages : UserControl
    {
        ISettings Settings = JsonSettings.Load<ISettings>().EnableAutosave();

        internal static Packages Instance;

        private string _wingetData = string.Empty;
        private static readonly object Lock = new();
        private string _TempSetupPath = string.Empty;
        public ObservableCollection<PackageModel> DataList { get; set; } = new ObservableCollection<PackageModel>();
        List<PackageModel> _temoList = new List<PackageModel>();
        List<VersionModel> _tempVersions = new List<VersionModel>();
        public DownloadService downloaderService;

        public Process _wingetProcess;
        public Packages()
        {
            InitializeComponent();
            Instance = this;
            DataContext = this;

            BindingOperations.EnableCollectionSynchronization(DataList, Lock);
            BindingOperations.EnableCollectionSynchronization(_temoList, Lock);
            BindingOperations.EnableCollectionSynchronization(_tempVersions, Lock);
            DownloadManifests();
            SetDataListGrouping();
        }

        private void SetDataListGrouping()
        {
            dataGrid.RowDetailsVisibilityMode = Settings.ShowExtraDetails;

            // Set Group for DataGrid
            if (Settings.GroupByPublisher)
            {
                DataList.ShapeView().GroupBy(x => x.Publisher).Apply();
            }
            else
            {
                DataList.ShapeView().ClearGrouping().Apply();
            }
        }

        public async void DownloadManifests(bool IsRefresh = false)
        {
            DataList?.Clear();
            _temoList?.Clear();
            _tempVersions?.Clear();
            prgStatus.Value = 0;
            tgBlock.IsChecked = false;
            prgStatus.IsIndeterminate = true;
            tgCancelDownload.Visibility = Visibility.Collapsed;

            MainWindow.Instance.CommandButtonsVisibility(Visibility.Collapsed);
            bool _isConnected = Helper.IsConnectedToInternet();
            if ((_isConnected && !Directory.Exists(Consts.ManifestPath)) || (_isConnected && IsRefresh is true))
            {
                if (IsRefresh)
                {
                    txtStatus.Text = "Refreshing Packages...";
                }
                var manifestUrl = Consts.WingetPkgsRepository;

                WebClient client = new WebClient();

                client.DownloadFileCompleted += Client_DownloadFileCompleted;
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                await client.DownloadFileTaskAsync(new Uri(manifestUrl), Consts.RootPath + @"\winget-pkgs-master.zip");
                
            }
            else if (Directory.Exists(Consts.ManifestPath))
            {
                if (!_isConnected && IsRefresh)
                {
                    Growl.WarningGlobal("Unable to connect to the Internet, we Load local packages.");
                }
                LoadLocalManifests();
            }
            else
            {
                Growl.ErrorGlobal("Unable to connect to the Internet");
            }
        }

        private async void LoadLocalManifests()
        {
            int _totalmanifestsCount = 0;
            int _currentManifestCount = 0;
            if (Directory.Exists(Consts.ManifestPath))
            {
                prgStatus.IsIndeterminate = false;

                await Task.Run(async () =>
                {
                    var manifests = Helper.EnumerateManifest(Consts.ManifestPath);
                    _totalmanifestsCount = manifests.Count();
                    var _installedApps = Helper.GetInstalledApps();
                    foreach (var item in manifests)
                    {
                        _currentManifestCount += 1;
                        DispatcherHelper.RunOnMainThread(delegate
                        {
                            prgStatus.Value = _currentManifestCount * 100 / _totalmanifestsCount;
                            txtStatus.Text = $"Parsing Manifests... {_currentManifestCount}/{_totalmanifestsCount}";
                        });

                        var file = File.ReadAllText(item);
                        var input = new StringReader(file);

                        var deserializer = new DeserializerBuilder().Build();
                        var yamlObject = deserializer.Deserialize(input);
                        var serializer = new SerializerBuilder().JsonCompatible().Build();

                        if (yamlObject != null)
                        {
                            var json = serializer.Serialize(yamlObject);
                            var yaml = JsonConvert.DeserializeObject<YamlPackageModel>(json);
                            if (yaml != null)
                            {
                                var installedVersion = string.Empty;
                                var isInstalled = false;
                                switch (Settings.IdentifyPackageMode)
                                {
                                    case IdentifyPackageMode.Off:
                                        isInstalled = false;
                                        installedVersion = string.Empty;
                                        break;
                                    case IdentifyPackageMode.Internal:
                                        var data = _installedApps.Where(x => x.DisplayName.Contains(yaml.Name)).Select(x => x.Version);
                                        isInstalled = data.Any();
                                        installedVersion = isInstalled ? $"Installed Version: {data.FirstOrDefault()}" : string.Empty;
                                        break;
                                    case IdentifyPackageMode.Wingetcli:
                                        isInstalled = await IsPackageInstalledWingetcliMode(yaml.Name);
                                        installedVersion = string.Empty;
                                        break;
                                }

                                var package = new PackageModel
                                {
                                    Publisher = yaml.Publisher,
                                    Name = yaml.Name,
                                    IsInstalled = isInstalled,
                                    Version = yaml.Version,
                                    Id = yaml.Id,
                                    Url = yaml.Installers[0].Url,
                                    Description = yaml.Description,
                                    LicenseUrl = yaml.LicenseUrl,
                                    Homepage = yaml.Homepage,
                                    Arch = yaml.Id + " " + yaml.Installers[0].Arch,
                                    InstalledVersion = installedVersion
                                };

                                if (!_temoList.Contains(package, new GenericCompare<PackageModel>(x => x.Name)))
                                {
                                    _temoList.Add(package);
                                }

                                _tempVersions.Add(new VersionModel { Id = package.Id, Version = package.Version });
                            }
                        }
                    }

                    foreach (var item in _temoList)
                    {
                        var _versions = _tempVersions.Where(v => v.Id == item.Id).Select(v => v.Version).OrderByDescending(v => v).ToList();

                        DataList.Add(new PackageModel
                        {
                            Id = item.Id,
                            Arch = item.Arch,
                            Description = item.Description,
                            Homepage = item.Homepage,
                            InstalledVersion = item.InstalledVersion,
                            IsInstalled = item.IsInstalled,
                            LicenseUrl = item.LicenseUrl,
                            Name = item.Name,
                            Publisher = item.Publisher,
                            Url = item.Url,
                            Versions = _versions,
                            Version = _versions[0]
                        });
                    }

                });

                tgBlock.IsChecked = true;
                DataList.ShapeView().OrderBy(x => x.Publisher).ThenBy(x => x.Name).Apply();
                MainWindow.Instance.txtStatus.Text = $"Available Packages: {DataList.Count} | Updated: {Settings.UpdatedDate}";
                MainWindow.Instance.CommandButtonsVisibility(Visibility.Visible);
            }
        }

        private void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            var progress = (int)e.ProgressPercentage;
            if (e.TotalBytesToReceive == -1 || e.TotalBytesToReceive == 0)
            {
                if (!prgStatus.IsIndeterminate)
                {
                    prgStatus.IsIndeterminate = true;
                }

                txtStatus.Text = $"Downloading Manifests... {Helper.ConvertBytesToMegabytes(e.BytesReceived)} MB";
            }
            else
            {
                if (prgStatus.IsIndeterminate)
                {
                    prgStatus.IsIndeterminate = false;
                }
                prgStatus.Value = progress;
                txtStatus.Text = $"Downloading {Helper.ConvertBytesToMegabytes(e.BytesReceived)} MB of {Helper.ConvertBytesToMegabytes(e.TotalBytesToReceive)} MB  -  {progress}%";
            }
        }

        private async void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                Settings.UpdatedDate = DateTime.Now;
                var fileName = Consts.RootPath + @"\winget-pkgs-master.zip";
                prgStatus.IsIndeterminate = true;
                prgStatus.Value = 0;
                txtStatus.Text = "Extracting Manifests...";
                await Task.Run(() => ZipFile.ExtractToDirectory(fileName, Consts.RootPath, true));
                txtStatus.Text = "Cleaning Directory...";
                MoveManifestToCorrectLocation(fileName);
                prgStatus.IsIndeterminate = false;
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        private async void MoveManifestToCorrectLocation(string FileName)
        {
           await Task.Run(()=> {
                var rootDir = new DirectoryInfo(Consts.RootPath + @"\winget-pkgs-master");
                var zipFile = new FileInfo(FileName);
                var pkgDir = new DirectoryInfo(Consts.ManifestPath);
                var moveDir = new DirectoryInfo(Consts.RootPath + @"\winget-pkgs-master\manifests");
                if (moveDir.Exists)
                {
                    if (pkgDir.Exists)
                    {
                        pkgDir.Delete(true);
                    }

                    moveDir.MoveTo(pkgDir.FullName);
                    rootDir.Delete(true);

                    if (zipFile.Exists)
                    {
                        zipFile.Delete();
                    }
                }
            });
            LoadLocalManifests();
        }

        private async void tgCancelDownload_Checked(object sender, RoutedEventArgs e)
        {
            if (tgCancelDownload.IsChecked.Value)
            {
                _wingetProcess?.Close();
                _wingetProcess?.Dispose();
                downloaderService?.CancelAsync();

                tgCancelDownload.IsEnabled = false;
                prgStatus.IsIndeterminate = true;
                prgStatus.ShowError = true;
                txtStatus.Text = "Operation Canceled";
                await Task.Delay(4000);
                tgBlock.IsChecked = true;
                MainWindow.Instance.CommandButtonsVisibility(Visibility.Visible);
            }
        }

        private async Task<bool> IsPackageInstalledWingetcliMode(string packageName)
        {
            if (string.IsNullOrEmpty(_wingetData))
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "winget",
                        Arguments = "list"
                    }
                };

                p.Start();
                _wingetData = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
            }

            if (_wingetData.Contains("Unrecognized command"))
            {
                Growl.ErrorGlobal("your Winget-cli is not supported please Update your winget-cli.");
                Helper.StartProcess(Consts.WingetRepository);
                return false;
            }

            return _wingetData.Contains(packageName);
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (MainWindow.Instance.appBarIsInstalled.IsChecked.Value)
            {
                DataList.ShapeView().Where(x =>
                        (x.IsInstalled && x.Name.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1) ||
                        (x.IsInstalled && x.Publisher.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) !=-1)).Apply();
            }
            else
            {
                DataList.ShapeView().Where(p =>
                         (p.Name.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1) ||
                         (p.Publisher.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1)).Apply();
            }

            var suggestions = new List<string>();
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var matchingItems = DataList.Where(p =>
                          (p.Name.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1) ||
                          (p.Publisher.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1));
                foreach (var item in matchingItems)
                {
                    suggestions.Add(item.Name);
                }
                if (suggestions.Count > 0)
                {
                    for (int i = 0; i < suggestions.Count; i++)
                    {
                        autoBox.ItemsSource = suggestions;
                    }
                }
                else
                {
                    autoBox.ItemsSource = new string[] { "No result found" };
                }
            }
        }

        public void FilterInstalledApps(bool ShowInstalled)
        {
            if (ShowInstalled)
            {
                DataList.ShapeView().Where(x => x.IsInstalled).Apply();
            }
            else
            {
                DataList.ShapeView().ClearFilter().Apply();
            }
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem button)
            {
                ContextMenuActions(button.Tag.ToString());
            }
        }

        private void ContextMenuActions(string tag)
        {
            var selectedRows = dataGrid.SelectedItems.Count;
            var item = (PackageModel)dataGrid.SelectedItem;
            string text = $"winget install {item?.Id} -v {item?.Version}";

            switch (tag)
            {
                case "SendToPow":
                    if (selectedRows > 1)
                    {
                        var script = CreatePowerShellScript(false);
                        Process.Start("powershell.exe", script);
                    }
                    else if (selectedRows == 1)
                    {
                        Process.Start("powershell.exe", text);
                    }
                    break;
                case "SendToCmd":
                    if (selectedRows == 1)
                    {
                        Interaction.Shell(text, AppWinStyle.NormalFocus);
                    }

                    break;
                case "Copy":
                    if (selectedRows == 1)
                    {
                        Clipboard.SetText(text);
                    }
                    break;
                case "Uninstall":
                    if (selectedRows == 1 && !string.IsNullOrEmpty(item.Name) && item.IsInstalled)
                    {
                        var result = Helper.UninstallPackage(item.Name);
                        if (!result)
                        {
                            Growl.InfoGlobal("Sorry, we were unable to uninstall your package");
                        }
                    }
                    break;
            }
        }

        private string CreatePowerShellScript(bool isExportScript)
        {
            StringBuilder builder = new StringBuilder();
            if (isExportScript)
            {
                builder.Append(Helper.PowerShellScript);
            }

            foreach (var item in dataGrid.SelectedItems)
            {
                builder.Append($"winget install {((PackageModel)item).Id} -v {((PackageModel)item).Version} -e ; ");
            }

            builder.Remove(builder.ToString().LastIndexOf(";"), 1);
            if (isExportScript)
            {
                builder.AppendLine("}");
            }

            return builder.ToString().TrimEnd();
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var selectedRows = dataGrid.SelectedItems.Count;

            if (selectedRows > 1)
            {
                mnuCmd.IsEnabled = false;
                mnuUninstall.IsEnabled = false;
                mnuSendToCmd.IsEnabled = false;
            }
            else
            {
                mnuCmd.IsEnabled = true;
                mnuSendToCmd.IsEnabled = true;
            }

            if (dataGrid.SelectedItem != null && ((PackageModel)dataGrid.SelectedItem).IsInstalled && selectedRows == 1)
            {
                mnuUninstall.IsEnabled = true;
            }
            else
            {
                mnuUninstall.IsEnabled = false;
            }
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.P)
            {
                ContextMenuActions("SendToPow");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.W)
            {
                ContextMenuActions("SendToCmd");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.C)
            {
                ContextMenuActions("Copy");
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.U)
            {
                ContextMenuActions("Uninstall");
            }
        }

        public async void ExportPowerShellScript()
        {
            if (dataGrid.SelectedItems.Count > 0)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Script",
                    FileName = "winget-script.ps1",
                    DefaultExt = "ps1",
                    Filter = "Powershell Script (*.ps1)|*.ps1"
                };
                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllTextAsync(dialog.FileName, CreatePowerShellScript(true));
                }
            }
            else
            {
                Growl.InfoGlobal("Please Select Packages");
            }
        }

        public void InstallPackage()
        {
            tgCancelDownload.Visibility = Visibility.Visible;
            tgCancelDownload.IsEnabled = true;
            tgCancelDownload.IsChecked = false;
            prgStatus.ShowError = false;
            prgStatus.IsIndeterminate = true;
            prgStatus.Value = 0;
            if (Helper.IsConnectedToInternet())
            {
                switch (Settings.InstallMode)
                {
                    case InstallMode.Wingetcli:
                        if (Helper.IsWingetInstalled())
                        {
                            InstallWingetMode();
                        }
                        break;
                    case InstallMode.Internal:
                        if (dataGrid.SelectedItems.Count > 1)
                        {
                            Growl.ErrorGlobal("you can not install more than 1 package in Internal Mode, for doing this please go to General and switch Install Mode from Internal to Winget-cli Mode.");
                        }
                        else
                        {
                            InstallInternalMode();
                        }
                        break;
                }
            }
            else
            {
                Growl.ErrorGlobal("Unable to connect to the Internet");
            }
        }

        private void InstallWingetMode()
        {
            var item = (PackageModel)dataGrid.SelectedItem;
            if (item != null && item.Id != null)
            {
                MainWindow.Instance.CommandButtonsVisibility(Visibility.Collapsed);

                tgBlock.IsChecked = false;
                prgStatus.IsIndeterminate = true;
                txtStatus.Text = $"Preparing to download {item.Id}";
                int selectedPackagesCount = 1;
                int currentCount = 0;
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                if (dataGrid.SelectedItems.Count > 1)
                {
                    var script = CreatePowerShellScript(false);
                    selectedPackagesCount = dataGrid.SelectedItems.Count;
                    startInfo.FileName = @"powershell.exe";
                    startInfo.Arguments = script;
                }
                else
                {
                    startInfo.FileName = @"winget";
                    startInfo.Arguments = $"install {item.Id} -v {item.Version}";
                }

                _wingetProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _wingetProcess.OutputDataReceived += (o, args) =>
                {
                    var line = args.Data ?? "";

                    DispatcherHelper.RunOnMainThread(() =>
                    {
                        if (line.Contains("Download"))
                        {
                            currentCount += 1;
                            txtStatus.Text = $"Downloading Package {currentCount}/{selectedPackagesCount}...";
                        }

                        if (line.Contains("hash"))
                        {
                            txtStatus.Text = $"Validated hash for {item.Id} {currentCount}/{selectedPackagesCount}";
                        }

                        if (line.Contains("Installing"))
                        {
                            txtStatus.Text = $"Installing {item.Id} {currentCount}/{selectedPackagesCount}";
                        }

                        if (line.Contains("Failed"))
                        {
                            txtStatus.Text = $"Installation of {item.Id} failed";
                        }
                    });
                };
                _wingetProcess.Exited += (o, _) =>
                {
                    Application.Current.Dispatcher.Invoke(async() =>
                    {
                        var installFailed = (o as Process).ExitCode != 0;
                        if (installFailed)
                        {
                            txtStatus.Text = $"Installation of {item.Id} failed";
                            prgStatus.ShowError = true;
                        }
                        else
                        {
                            txtStatus.Text = $"Installed {item.Id}";
                        }

                        await Task.Delay(4500);
                        tgBlock.IsChecked = true;
                        prgStatus.ShowError = false;
                        prgStatus.IsIndeterminate = false;

                        MainWindow.Instance.CommandButtonsVisibility(Visibility.Visible);
                    });
                };

                _wingetProcess.Start();
                _wingetProcess.BeginOutputReadLine();
            }
            else
            {
                Growl.InfoGlobal("Please select an application!");
            }
        }

        public async void InstallInternalMode()
        {
            try
            {
                var item = (PackageModel)dataGrid.SelectedItem;

                if (item != null && item.Id != null)
                {
                    MainWindow.Instance.CommandButtonsVisibility(Visibility.Collapsed);

                    var url = Helper.RemoveComment(item.Url);

                    if (Settings.IsIDMEnabled)
                    {
                        Helper.DownloadWithIDM(url);
                    }
                    else
                    {
                        txtStatus.Text = $"Preparing to download {item.Id}";
                        tgBlock.IsChecked = false;
                        prgStatus.IsIndeterminate = false;
                        _TempSetupPath =$@"{Consts.TempSetupPath}\{item.Id}-{item.Version}{Helper.GetExtension(url)}".Trim();
                        if (!File.Exists(_TempSetupPath))
                        {
                            downloaderService = new DownloadService();
                            downloaderService.DownloadProgressChanged += DownloaderService_DownloadProgressChanged;
                            downloaderService.DownloadFileCompleted += DownloaderService_DownloadFileCompleted;
                            await downloaderService.DownloadFileAsync(url, _TempSetupPath);
                        }
                        else
                        {
                            tgBlock.IsChecked = true;
                            Helper.StartProcess(_TempSetupPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal(ex.Message);
            }
        }

        private void DownloaderService_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Helper.StartProcess(_TempSetupPath);
            DispatcherHelper.RunOnMainThread(() => {
                MainWindow.Instance.CommandButtonsVisibility(Visibility.Visible);
            });
        }

        private void DownloaderService_DownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() => {
                prgStatus.Value = (int)e.ProgressPercentage;
                var item = (PackageModel)dataGrid.SelectedItem;
                txtStatus.Text = $"Downloading {item.Id}-{item.Version} - {Helper.ConvertBytesToMegabytes(e.ReceivedBytesSize)} MB of {Helper.ConvertBytesToMegabytes(e.TotalBytesToReceive)} MB  -   {(int)e.ProgressPercentage}%";
            });
        }
    }
}
