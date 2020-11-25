using HandyWinGet.Views;
using Microsoft.Win32;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace HandyWinGet
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (!IsOSSupported())
            {
                HandyControl.Controls.MessageBox.Error("your Windows Is Not Supported Please Update to Windows 10 1709 (build 16299) or later");
                Environment.Exit(0);
            }

            if (!IsWingetInstalled())
            {
                HandyControl.Controls.MessageBox.Error("Winget-cli Is Not Installed, Please Install it first", "Install Winget");
                ProcessStartInfo ps = new ProcessStartInfo("https://github.com/microsoft/winget-cli/releases")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
                Environment.Exit(0);
            }

        }
        protected override void InitializeShell(DependencyObject shell)
        {
            base.InitializeShell(shell);
            Container.Resolve<IRegionManager>().RequestNavigate("ContentRegion", "Packages");

        }
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<CreatePackage>();
            containerRegistry.RegisterForNavigation<Packages>();
            containerRegistry.RegisterForNavigation<Settings>();
        }

        public bool IsWingetInstalled()
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

        public bool IsOSSupported()
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
