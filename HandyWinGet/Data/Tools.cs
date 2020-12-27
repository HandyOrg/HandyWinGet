using HandyControl.Controls;
using HandyWinGet.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace HandyWinGet.Data
{
    public class Tools
    {
        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }


        public static string ConvertBytesToMegabytes(long bytes)
        {
            return ((bytes / 1024f) / 1024f).ToString("0.00");
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
            var IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
            var IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";
            if (File.Exists(IDManX64Location))
            {
                Process.Start(IDManX64Location, command);
            }
            else if (File.Exists(IDManX86Location))
            {
                Process.Start(IDManX86Location, command);
            }
            else
            {
                Growl.ErrorGlobal(
                    "Internet Download Manager (IDM) is not installed on your system, please download and install it first");
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
            foreach (var file in Directory.GetFiles(directory))
                yield return file;
        }
        
        public static void FindInstalledApps(RegistryKey regKey, List<string> keys, List<InstalledAppModel> installed)
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
                                Publisher = (string)sk.GetValue("Publisher"),
                                UnninstallCommand = (string)sk.GetValue("UninstallString")
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
