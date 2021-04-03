using Downloader;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyControl.Tools.Extension;
using HandyWinget.Assets;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static HandyWinget.Assets.Helper;
namespace HandyWinget.Views
{
    public partial class Packages : UserControl
    {
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
            Loaded += Packages_Loaded;
            BindingOperations.EnableCollectionSynchronization(DataList, Lock);
            BindingOperations.EnableCollectionSynchronization(_temoList, Lock);
            BindingOperations.EnableCollectionSynchronization(_tempVersions, Lock);
        }

        private async void Packages_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Settings.Version.Equals(RegistryHelper.GetValue<int>(Consts.VersionKey, Consts.AppName)))
            {
                if (Directory.Exists(Consts.ManifestPath))
                {
                    txtStatus.Text = "Please wait...";
                    Directory.Delete(Consts.ManifestPath, true);
                    await Task.Delay(10000);
                }
                RegistryHelper.AddOrUpdateKey(Consts.VersionKey, Consts.AppName, Settings.Version);
            }
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
            bool _isConnected = ApplicationHelper.IsConnectedToInternet();
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
            if (Directory.Exists(Consts.ManifestPath))
            {
                prgStatus.IsIndeterminate = false;

                await Task.Run(async () =>
                {
                    var manifests = EnumerateManifest(Consts.ManifestPath);
                    _totalmanifestsCount = manifests.Count();
                    var _installedApps = GetInstalledApps();
                    foreach (var item in manifests.GetEnumeratorWithIndex())
                    {
                        try
                        {
                            DispatcherHelper.RunOnMainThread(delegate
                            {
                                prgStatus.Value = item.Index * 100 / _totalmanifestsCount;
                                txtStatus.Text = $"Parsing Manifests... {item.Index}/{_totalmanifestsCount}";
                            });

                            if (item.Value.Contains(".installer.yaml") || item.Value.Contains(".locale."))
                            {
                                continue;
                            }

                            var deserializer = new DeserializerBuilder()
                                                        .WithNamingConvention(PascalCaseNamingConvention.Instance)
                                                        .IgnoreUnmatchedProperties()
                                                        .Build();

                            var baseFolder = Path.GetDirectoryName(item.Value);
                            var result = deserializer.Deserialize<YamlPackageModel>(File.OpenText(item.Value));

                            var installedVersion = string.Empty;
                            var isInstalled = false;

                            PackageModel package = null;
                            List<Installer> installer = new List<Installer>();
                            string publisher = string.Empty;
                            string packageName = string.Empty;
                            string packageVersion = string.Empty;
                            string packageIdentifier = string.Empty;
                            string description = string.Empty;
                            string licenseUrl = string.Empty;
                            string license = string.Empty;
                            string homePage = string.Empty;

                            if (result != null)
                            {
                                if (result.ManifestType.Contains("singleton"))
                                {
                                    package = new PackageModel
                                    {
                                        Publisher = result.Publisher,
                                        PackageName = result.PackageName,
                                        IsInstalled = isInstalled,
                                        PackageIdentifier = result.PackageIdentifier,
                                        Description = result.ShortDescription,
                                        LicenseUrl = result.LicenseUrl,
                                        Homepage = result.PackageUrl,
                                        InstalledVersion = installedVersion
                                    };
                                    installer = result.Installers;
                                    packageVersion = result.PackageVersion;
                                }
                                else
                                {
                                    var localPath = item.Value.Replace(".yaml", ".locale.en-US.yaml");
                                    var installerPath = item.Value.Replace(".yaml", ".installer.yaml");

                                    if (File.Exists(localPath))
                                    {
                                        var multiYamlResult = deserializer.Deserialize<YamlPackageModel>(File.OpenText(localPath));
                                        packageIdentifier = multiYamlResult.PackageIdentifier;
                                        packageVersion = multiYamlResult.PackageVersion;
                                        publisher = multiYamlResult.Publisher;
                                        packageName = multiYamlResult.PackageName;
                                        licenseUrl = multiYamlResult.LicenseUrl;
                                        license = multiYamlResult.License;
                                        description = multiYamlResult.ShortDescription;
                                    }

                                    if (File.Exists(installerPath))
                                    {
                                        var multiYamlResult = deserializer.Deserialize<YamlPackageModel>(File.OpenText(installerPath));
                                        installer = multiYamlResult.Installers;
                                    }

                                    package = new PackageModel
                                    {
                                        Publisher = publisher,
                                        PackageName = packageName,
                                        IsInstalled = isInstalled,
                                        PackageIdentifier = packageIdentifier,
                                        Description = description,
                                        LicenseUrl = licenseUrl,
                                        Homepage = homePage,
                                        InstalledVersion = installedVersion
                                    };
                                }

                                switch (Settings.IdentifyPackageMode)
                                {
                                    case IdentifyPackageMode.Off:
                                        isInstalled = false;
                                        installedVersion = string.Empty;
                                        break;
                                    case IdentifyPackageMode.Internal:
                                        var data = _installedApps.Where(x => x.DisplayName.Contains(result.PackageName)).Select(x => x.Version);
                                        isInstalled = data.Any();
                                        installedVersion = isInstalled ? $"Installed Version: {data.FirstOrDefault()}" : string.Empty;
                                        break;
                                    case IdentifyPackageMode.Wingetcli:
                                        isInstalled = await IsPackageInstalledWingetcliMode(result.PackageName);
                                        installedVersion = string.Empty;
                                        break;
                                }

                                if (!_temoList.Contains(package, new GenericCompare<PackageModel>(x => x.PackageName)))
                                {
                                    _temoList.Add(package);
                                }

                                _tempVersions.Add(new VersionModel { Id = package.PackageIdentifier, Version = packageVersion, Installers = installer});

                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    foreach (var item in _temoList)
                    {
                        try
                        {
                            var _versionsAndArchitecture = _tempVersions.Where(v => v.Id == item.PackageIdentifier).OrderByDescending(v => v.Version).ToList();

                            DataList.Add(new PackageModel
                            {
                                PackageIdentifier = item.PackageIdentifier,
                                Description = item.Description,
                                Homepage = item.Homepage,
                                InstalledVersion = item.InstalledVersion,
                                IsInstalled = item.IsInstalled,
                                LicenseUrl = item.LicenseUrl,
                                PackageName = item.PackageName,
                                Publisher = item.Publisher,
                                Versions = _versionsAndArchitecture,
                                PackageVersion = _versionsAndArchitecture[0],
                                PackageArchitecture = _versionsAndArchitecture[0].Installers[0]
                            });
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                });

                tgBlock.IsChecked = true;
                DataList.ShapeView().OrderBy(x => x.Publisher).ThenBy(x => x.PackageName).Apply();
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

                txtStatus.Text = $"Downloading Manifests... {ConvertBytesToMegabytes(e.BytesReceived)} MB";
            }
            else
            {
                if (prgStatus.IsIndeterminate)
                {
                    prgStatus.IsIndeterminate = false;
                }
                prgStatus.Value = progress;
                txtStatus.Text = $"Downloading {ConvertBytesToMegabytes(e.BytesReceived)} MB of {ConvertBytesToMegabytes(e.TotalBytesToReceive)} MB  -  {progress}%";
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
           await Task.Run(async ()=> {
                var rootDir = new DirectoryInfo(Consts.RootPath + @"\winget-pkgs-master");
                var zipFile = new FileInfo(FileName);
                var pkgDir = new DirectoryInfo(Consts.ManifestPath);
                var moveDir = new DirectoryInfo(Consts.RootPath + @"\winget-pkgs-master\manifests");
                await Task.Delay(3000);

               try
               {
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
               }
               catch (IOException)
               {
                   Growl.ErrorGlobal("Something is wrong, please try again.");
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
            if (packageName.IsNullOrEmpty())
            {
                return false;
            }

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
                        (x.IsInstalled && x.PackageName != null && x.PackageName.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1) ||
                        (x.IsInstalled && x.Publisher != null && x.Publisher.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) !=-1)).Apply();
            }
            else
            {
                DataList.ShapeView().Where(p =>
                         (p.PackageName != null && p.PackageName.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1) ||
                         (p.Publisher != null && p.Publisher.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1)).Apply();
            }

            var suggestions = new List<string>();
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var matchingItems = DataList.Where(p =>
                          (p.PackageName != null && p.PackageName.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1) ||
                          (p.Publisher != null && p.Publisher.IndexOf(autoBox.Text, StringComparison.OrdinalIgnoreCase) != -1));
                foreach (var item in matchingItems)
                {
                    suggestions.Add(item.PackageName);
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
            var item = GetSelectedPackage();
            string text = $"winget install {item.PackageIdentifier} -v {item.Version}";

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
                    if (selectedRows == 1 && !string.IsNullOrEmpty(item.PackageName) && item.IsInstalled)
                    {
                        var result = UninstallPackage(item.PackageName);
                        if (!result)
                        {
                            Growl.InfoGlobal("Sorry, we were unable to uninstall your package");
                        }
                    }
                    break;
                case "Export":
                    ExportPowerShellScript();
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
                builder.Append($"winget install {((PackageModel)item).PackageIdentifier} -v {((PackageModel)item).PackageVersion.Version} -e ; ");
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
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.X)
            {
                ContextMenuActions("Export");
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
            if (ApplicationHelper.IsConnectedToInternet())
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
            var item = GetSelectedPackage();
            if (item.PackageIdentifier != null)
            {
                MainWindow.Instance.CommandButtonsVisibility(Visibility.Collapsed);

                tgBlock.IsChecked = false;
                prgStatus.IsIndeterminate = true;
                txtStatus.Text = $"Preparing to download {item.PackageIdentifier}";
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
                    startInfo.Arguments = $"install {item.PackageIdentifier} -v {item.Version}";
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
                            txtStatus.Text = $"Validated hash for {item.PackageIdentifier} {currentCount}/{selectedPackagesCount}";
                        }

                        if (line.Contains("Installing"))
                        {
                            txtStatus.Text = $"Installing {item.PackageIdentifier} {currentCount}/{selectedPackagesCount}";
                        }

                        if (line.Contains("Failed"))
                        {
                            txtStatus.Text = $"Installation of {item.PackageIdentifier} failed";
                        }

                        if (line.Contains("Please update the client"))
                        {
                            txtStatus.Text = $"Installation of {item.PackageIdentifier} failed, Please update the winget-cli client.";

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
                            txtStatus.Text = $"Installation of {item.PackageIdentifier} failed";
                            prgStatus.ShowError = true;
                        }
                        else
                        {
                            txtStatus.Text = $"Installed {item.PackageIdentifier}";
                        }

                        await Task.Delay(5000);
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
                var item = GetSelectedPackage();

                if (item.PackageIdentifier != null)
                {
                    MainWindow.Instance.CommandButtonsVisibility(Visibility.Collapsed);

                    var url = RemoveComment(item.InstallerUrl);

                    if (Settings.IsIDMEnabled)
                    {
                        DownloadWithIDM(url);
                    }
                    else
                    {
                        txtStatus.Text = $"Preparing to download {item.PackageIdentifier}";
                        tgBlock.IsChecked = false;
                        prgStatus.IsIndeterminate = false;
                        _TempSetupPath = $@"{Consts.TempSetupPath}\{item.PackageIdentifier}-{item.Version}{GetExtension(url)}".Trim();
                        if (!File.Exists(_TempSetupPath))
                        {
                            downloaderService = new DownloadService();
                            downloaderService.DownloadProgressChanged += DownloaderService_DownloadProgressChanged;
                            downloaderService.DownloadFileCompleted += DownloaderService_DownloadFileCompleted;
                            await downloaderService.DownloadFileTaskAsync(url, _TempSetupPath);
                        }
                        else
                        {
                            tgBlock.IsChecked = true;
                            StartProcess(_TempSetupPath);
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
            DispatcherHelper.RunOnMainThread(() => {
                MainWindow.Instance.CommandButtonsVisibility(Visibility.Visible);
                tgBlock.IsChecked = true;
            });
            StartProcess(_TempSetupPath);
        }

        private void DownloaderService_DownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() => {
                prgStatus.Value = (int)e.ProgressPercentage;
                var item = GetSelectedPackage();
                txtStatus.Text = $"Downloading {item.PackageIdentifier}-{item.Version} - {ConvertBytesToMegabytes(e.ReceivedBytesSize)} MB of {ConvertBytesToMegabytes(e.TotalBytesToReceive)} MB  -   {(int)e.ProgressPercentage}%";
            });
        }

        public (string PackageIdentifier, string Version, string Architecture, string InstallerUrl, string PackageName, bool IsInstalled) GetSelectedPackage()
        {
            var pkg = dataGrid.SelectedItem as PackageModel;
            string id = string.Empty;
            string version = string.Empty;
            string arch = string.Empty;
            string url = string.Empty;
            string pName = string.Empty;
            bool isInstalled = false;

            if (pkg != null && pkg.PackageIdentifier != null)
            {
                id = pkg.PackageVersion.Id;
                version = pkg.PackageVersion.Version;
                arch = pkg.PackageArchitecture.Architecture;
                url = pkg.PackageArchitecture.InstallerUrl;
                pName = pkg.PackageName;
                isInstalled = pkg.IsInstalled;
                
            }
            return (id, version, arch, url, pName, isInstalled);
        }
    }
}
