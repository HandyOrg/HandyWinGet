using HandyControl.Controls;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace HandyWinget.Assets
{
    public abstract class Helper
    {
        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool IsConnectedToInternet()
        {
            return InternetGetConnectedState(out int Desc, 0);
        }
        public static string ConvertBytesToMegabytes(long bytes)
        {
            return ((bytes / 1024f) / 1024f).ToString("0.00");
        }
        public static Color GetColorFromBrush(Brush brush)
        {
            if (brush.GetType() == typeof(LinearGradientBrush))
            {
                var linearBrush = (LinearGradientBrush)brush;
                return new SolidColorBrush(linearBrush.GradientStops[1].Color).Color;
            }
            else
            {
                return ((SolidColorBrush)brush).Color;
            }
        }

        public static bool IsWingetInstalled()
        {
            if (IsWingetExist())
            {
                return true;
            }
            else
            {
                Growl.AskGlobal("Winget-cli is not installed, please download and install latest version.", b =>
                {
                    if (!b)
                    {
                        return true;
                    }
                    StartProcess(Consts.WingetRepository);
                    return true;
                });

                return false;
            }
            
        }

        private static bool IsWingetExist()
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
                Growl.ErrorGlobal("Your Windows Is Not Supported, Winget-cli requires Windows 10 version 1709 (build 16299) Please Update to Windows 10 1709 (build 16299) or later");
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
            catch (Win32Exception ex)
            {
                if (!ex.Message.Contains("The system cannot find the file specified."))
                {
                    Growl.ErrorGlobal(ex.Message);
                }
            }
        }
        public static IEnumerable<string> EnumerateManifest(string rootDirectory)
        {
            foreach (var directory in Directory.GetDirectories(
                rootDirectory,
                "*",
                SearchOption.AllDirectories))
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    yield return file;
                }
            }
        }
        private static readonly List<string> _keys = new()
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };
        private static List<InstalledAppModel> _installedApps = new();

        public static List<InstalledAppModel> GetInstalledApps()
        {
            FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), _keys, _installedApps);
            FindInstalledApps(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64), _keys, _installedApps);
            return _installedApps.Distinct().ToList();
        }

        private static void FindInstalledApps(RegistryKey regKey, List<string> keys, List<InstalledAppModel> installed)
        {
            foreach (var key in keys)
            {
                using var rk = regKey.OpenSubKey(key);
                if (rk == null)
                {
                    continue;
                }

                foreach (var skName in rk.GetSubKeyNames())
                {
                    using var sk = rk.OpenSubKey(skName);
                    if (sk?.GetValue("DisplayName") != null)
                    {
                        try
                        {
                            installed.Add(new InstalledAppModel
                            {
                                DisplayName = (string)sk.GetValue("DisplayName"),
                                Version = (string)sk.GetValue("DisplayVersion"),
                                Publisher = (string)sk.GetValue("Publisher")
                            });
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
            }
        }
        public static bool UninstallPackage(string packageName)
        {
            var result = FindUninstallString(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64), packageName);
            try
            {
                if (result.Item1)
                {
                    Interaction.Shell(MakeUninstallString(result.Item2), AppWinStyle.NormalFocus);
                    return true;
                }
                else
                {
                    result = FindUninstallString(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), packageName);
                    if (result.Item1)
                    {
                        Interaction.Shell(MakeUninstallString(result.Item2), AppWinStyle.NormalFocus);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            return false;
        }

        private static string MakeUninstallString(string uninstallstring)
        {
            if (uninstallstring.Substring(0, 1).Equals("\"") | uninstallstring.ToLower().Contains("msiexec") | uninstallstring.Contains("~"))
            {
                //ignore
            }
            else if (uninstallstring.ToLower().IndexOf(".exe") > 0)
            {
                uninstallstring = "\"" + uninstallstring.Insert(uninstallstring.ToLower().IndexOf(".exe") + 4, "\"");
            }
            else
            {
                uninstallstring = "\"" + uninstallstring + "\"";
            }

            return uninstallstring;
        }
        private static (bool, string) FindUninstallString(RegistryKey regKey, string packageName)
        {
            foreach (var key in _keys)
            {
                using var rk = regKey.OpenSubKey(key);
                if (rk == null)
                {
                    continue;
                }

                foreach (var skName in rk.GetSubKeyNames())
                {
                    using var sk = rk.OpenSubKey(skName);
                    if (sk?.GetValue("DisplayName") != null)
                    {
                        string displayName = (string)sk.GetValue("DisplayName");
                        if (displayName.Contains(packageName))
                        {
                            string uninstall = (string)sk.GetValue("UninstallString");

                            return (true, uninstall);
                        }
                    }
                }
            }

            return (false, string.Empty);
        }

        public static string RemoveComment(string url)
        {
            var index = url.IndexOf("#");
            if (index >= 0)
            {
                return url.Substring(0, index).Trim();
            }

            return url.Trim();
        }

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
                Growl.ErrorGlobal("Internet Download Manager (IDM) is not installed on your system, please download and install it first");
            }
        }
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
      $file = ($json | ConvertFrom-Json)[0].assets[0].name
	
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
