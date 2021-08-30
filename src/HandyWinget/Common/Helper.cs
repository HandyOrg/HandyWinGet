using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Controls;
using HandyControl.Tools;
using HandyWinget.Control;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Autosave;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;
using MessageBox = HandyControl.Controls.MessageBox;
using Path = System.IO.Path;

namespace HandyWinget.Common
{
    public static class Helper
    {
        public static HWGSettings Settings = JsonSettings.Configure<HWGSettings>()
                                   .WithRecovery(RecoveryAction.RenameAndLoadDefault)
                                   .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
                                   .LoadNow()
                                   .EnableAutosave();

        /// <summary>
        /// Remove Comment from Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string RemoveUrlComment(string url)
        {
            var index = url.IndexOf("#");
            if (index >= 0)
            {
                return url.Substring(0, index).Trim();
            }

            return url.Trim();
        }

        public static bool UninstallPackage(string productCode)
        {
            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "winget",
                        Arguments = $@"uninstall --id ""{productCode}"""
                    }
                };
                p.Start();
                var _wingetData = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                if (_wingetData.Contains("Unrecognized command"))
                {
                    return false;
                }
                if (_wingetData.Contains("successfully uninstalled", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
            }
            return false;
        }

        public static bool UpgradePackage(string packageId)
        {
            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "winget",
                        Arguments = $@"upgrade --id {packageId}"
                    }
                };
                p.Start();
                var _wingetData = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                if (_wingetData.Contains("Unrecognized command"))
                {
                    return false;
                }
                if (_wingetData.Contains("successfully installed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
            }
            return false;
        }

        public static void CreateColorPicker()
        {
            SolidColorBrush tempAccent = null;
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            var window = new PopupWindow
            {
                PopupElement = picker,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                MinWidth = 0,
                MinHeight = 0,
                Title = "Accent Color",
                FontFamily = ResourceHelper.GetResource<FontFamily>("CascadiaCode")
            };
            Style style = new Style(typeof(Button));
            style.BasedOn = ResourceHelper.GetResource<Style>("ButtonDefault");
            window.Resources.Add(typeof(Button), style);

            if (Settings.Accent != null)
            {
                picker.SelectedBrush = new SolidColorBrush(ColorHelper.GetColorFromBrush(Settings.Accent));
                tempAccent = picker.SelectedBrush;
            }

            picker.SelectedColorChanged += delegate
            {
                ((App) Application.Current).UpdateAccent(picker.SelectedBrush);
            };

            bool isConfirmed = false;
            picker.Confirmed += delegate
            {
                Settings.Accent = picker.SelectedBrush;
                isConfirmed = true;
                window.Close();
            };

            picker.Canceled += delegate
            {
                ((App) Application.Current).UpdateAccent(tempAccent);
                Settings.Accent = tempAccent;
                isConfirmed = false;
                window.Close();
            };

            window.Closing += (s, e) =>
            {
                if (!isConfirmed)
                {
                    ((App) Application.Current).UpdateAccent(tempAccent);
                    Settings.Accent = tempAccent;
                }
            };
            window.Show();
        }

        public static T ParseEnum<T>(string value)
        {
            return (T) Enum.Parse(typeof(T), value, true);
        }

        public static InfoBar CreateInfoBar(string title, string message, StackPanel panel, Severity severity)
        {
            if (Settings.IsShowNotifications)
            {
                var bar = new InfoBar();
                bar.Severity = severity;
                bar.Title = title;
                bar.Message = message;
                bar.Margin = new Thickness(0, 5, 0, 0);

                panel.Children.Insert(0, bar);
                return bar;
            }
            else
            {
                return null;
            }
        }

        public static InfoBar CreateInfoBarWithAction(string title, string message, StackPanel panel, Severity severity, string buttonContent, Action action)
        {
            if (Settings.IsShowNotifications)
            {
                var bar = new InfoBar();
                bar.Severity = severity;
                bar.Title = title;
                bar.Message = message;
                bar.Margin = new Thickness(0, 5, 0, 0);

                var btnAction = new Button();
                btnAction.Content = buttonContent;
                btnAction.Click += (e, s) => { action(); };

                bar.ActionButton = btnAction;
                panel.Children.Insert(0, bar);
                return bar;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Convert bytes to megabytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string BytesToMegabytes(long bytes)
        {
            return ((bytes / 1024f) / 1024f).ToString("0.00");
        }

        /// <summary>
        /// Check if winget-cli is installed or not
        /// </summary>
        /// <returns></returns>
        public static bool IsWingetInstalled()
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                proc.Start();
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        public static bool IsOsSupported()
        {
            if (OSVersionHelper.IsWindows10_1709_OrGreater)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void StartProcess(string path)
        {
            try
            {
                var ps = new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Win32Exception)
            {
            }
        }

        /// <summary>
        /// Execute winget list command and return all installed app details
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetInstalledAppList()
        {
            var p = new Process
            {
                StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "winget",
                        Arguments = $"list"
                    }
            };
            p.Start();
            var _wingetData = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            if (_wingetData.Contains("Unrecognized command"))
            {
                return null;
            }
            try
            {
                string input = _wingetData.Substring(_wingetData.IndexOf("Name"));
                var lines = input.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Skip(2);
                return lines;

            }
            catch (ArgumentOutOfRangeException)
            {

            }
            return null;
        }

        /// <summary>
        /// Parse and Find PackageId, Version and Available Version for Installed App
        /// </summary>
        /// <param name="line">installed app line details, extracted from winget list command</param>
        /// <param name="packageId"></param>
        /// <returns></returns>
        private static Regex re = new Regex("[ ]{2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static (string packageId, string version, string availableVersion) ParseInstalledApp(string line, string packageId)
        {
            line = re.Replace(line, " ");
            int startIndex = line.IndexOf(packageId);
            if(startIndex < 0)
            {
                return (packageId: null, version: null, availableVersion: null);
            }
            line = line.Substring(startIndex);
            var lines = line.Split(" ");
            if (lines.Count() >= 3) // available version exist
            {
                return (packageId: lines[0], version: lines[1], availableVersion: lines[2]);
            }
            else if (lines.Count() == 2) // only version exist
            {
                return (packageId: lines[0], version: lines[1], availableVersion: null);
            }
            return (packageId: null, version: null, availableVersion: null);
        }

        /// <summary>
        /// Pass Url to Internet Download Manager
        /// </summary>
        /// <param name="link"></param>
        public static void DownloadWithIDM(string link)
        {
            var command = $"/C /d \"{link}\"";
            if (File.Exists(Consts.IDManX64Location))
            {
                Process.Start(Consts.IDManX64Location, command);
            }
            else if (File.Exists(Consts.IDManX86Location))
            {
                Process.Start(Consts.IDManX86Location, command);
            }
            else
            {
                MessageBox.Error("Internet Download Manager (IDM) is not installed on your system, please download and install it first", "Not Found");
            }
        }

        /// <summary>
        /// Get URL Extension
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetExtension(string url)
        {
            var ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext))
            {
                var pointChar = ".";
                var slashChar = "/";

                var pointIndex = url.LastIndexOf(pointChar);
                var slashIndex = url.LastIndexOf(slashChar);

                if (pointIndex >= 0)
                {
                    if (slashIndex >= 0)
                    {
                        var pFrom = pointIndex + pointChar.Length;
                        var pTo = slashIndex;
                        return $".{url.Substring(pFrom, pTo - pFrom)}";
                    }

                    return url.Substring(pointIndex + pointChar.Length);
                }

                return string.Empty;
            }

            if (ext.Contains("?"))
            {
                var qTo = ext.IndexOf("?");
                return ext.Substring(0, qTo - 0);
            }

            return ext;
        }

        public static string PowerShellScript = @"
Write-Host ""Checking winget...""
Try{
      # Check if winget is already installed
	  $er = (invoke-expression ""winget -v"") 2>&1
      if ($lastexitcode) {throw $er}
      Write-Host ""winget is already installed.""
    }
Catch{
      # winget is not installed. Install it from the Github release
	  Write-Host ""winget is not found, installing it right now.""
      $repo = ""microsoft/winget-cli""
      $releases = ""https://api.github.com/repos/$repo/releases""
	
      Write-Host ""Determining latest release""
      $json = Invoke-WebRequest $releases
      $tag = ($json | ConvertFrom-Json)[0].tag_name
      $file = ($json | ConvertFrom-Json)[0].Common[0].name
	
      $download = ""https://github.com/$repo/releases/download/$tag/$file""
      $output = $PSScriptRoot + ""\winget-latest.appxbundle""
      Write-Host ""Dowloading latest release""
      Invoke-WebRequest -Uri $download -OutFile $output

      Write-Host ""Installing the package""
      Add-AppxPackage -Path $output
    }
Finally{            
";
    }
}
