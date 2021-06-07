using HandyControl.Controls;
using HandyControl.Tools;
using Microsoft.VisualBasic;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Autosave;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace HandyWinget.Assets
{
    public static class Helper
    {
        public static ISettings Settings = JsonSettings.Configure<ISettings>()
                                   .WithRecovery(RecoveryAction.RenameAndLoadDefault)
                                   .WithVersioning(new Version(1,0,0,0), VersioningResultAction.RenameAndLoadDefault)
                                   .LoadNow()
                                   .EnableAutosave();

        public static string ConvertBytesToMegabytes(long bytes)
        {
            return ((bytes / 1024f) / 1024f).ToString("0.00");
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

        #region Uninstall Package
        public static bool UninstallPackage(string uninstallString)
        {
            try
            {
                Interaction.Shell(uninstallString, AppWinStyle.NormalFocus);
                return true;
            }
            catch (FileNotFoundException)
            {
            }
            return false;
        }
        
        #endregion

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
