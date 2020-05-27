using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HandyWinget_GUI
{
    public class Tools
    {
        public static void DeleteDirectory(string d)
        {
            if (System.IO.Directory.Exists(d))
            {
                foreach (string sub in System.IO.Directory.EnumerateDirectories(d))
                {
                    DeleteDirectory(sub);
                }
                foreach (string f in System.IO.Directory.EnumerateFiles(d))
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(f)
                    {
                        Attributes = FileAttributes.Normal
                    };
                    fi.Delete();
                }
                System.IO.Directory.Delete(d);
            }
        }

        public static bool IsWingetInstalled()
        {
            try
            {
                Process proc = new Process
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

        public static bool IsSoftwareInstalled(string softwareName, string softwareVersion)
        {
            string registryUninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            string registryUninstallPathFor32BitOn64Bit = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            if (Is32BitWindows())
            {
                return IsSoftwareInstalled(softwareName, softwareVersion, RegistryView.Registry32, registryUninstallPath);
            }

            bool is64BitSoftwareInstalled = IsSoftwareInstalled(softwareName, softwareVersion, RegistryView.Registry64, registryUninstallPath);
            bool is32BitSoftwareInstalled = IsSoftwareInstalled(softwareName, softwareVersion, RegistryView.Registry64, registryUninstallPathFor32BitOn64Bit);
            return is64BitSoftwareInstalled || is32BitSoftwareInstalled;
        }

        private static bool Is32BitWindows()
        {
            return Environment.Is64BitOperatingSystem == false;
        }

        private static bool IsSoftwareInstalled(string softwareName, string softwareVersion, RegistryView registryView, string installedProgrammsPath)
        {
            RegistryKey uninstallKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView)
                                                  .OpenSubKey(installedProgrammsPath);

            if (uninstallKey == null)
            {
                return false;
            }

            return uninstallKey.GetSubKeyNames()
                               .Select(installedSoftwareString => uninstallKey.OpenSubKey(installedSoftwareString))
                               .Select(installedSoftwareKey => new
                               {
                                   Name = installedSoftwareKey.GetValue("DisplayName"),
                                   Version = installedSoftwareKey.GetValue("DisplayVersion")
                               })
                               .Where(x => x != null && x.Name != null && x.Version != null)
                               .Any(installedSoftwareName => installedSoftwareName != null && installedSoftwareName.Name.ToString().Contains(softwareName) && installedSoftwareName.Version.ToString().Contains(softwareVersion));
        }

        public static bool IsOSSupported()
        {
            string subKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion";
            RegistryKey key = Registry.LocalMachine;
            RegistryKey skey = key.OpenSubKey(subKey);

            string name = skey.GetValue("ProductName").ToString();
            if (name.Contains("Windows 10"))
            {
                int releaseId = Convert.ToInt32(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", ""));
                if (releaseId < 1709)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
