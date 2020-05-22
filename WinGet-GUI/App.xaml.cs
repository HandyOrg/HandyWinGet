using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using Microsoft.Win32;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Diagnostics;
using System.Windows;
using WinGet_GUI.Assets;
using WinGet_GUI.Assets.Languages;
using WinGet_GUI.Views;

namespace WinGet_GUI
{
    public partial class App
    {
        public App()
        {
            GlobalDataHelper<AppConfig>.Init();
            LocalizationManager.Instance.LocalizationProvider = new ResxProvider();
            LocalizationManager.Instance.CurrentCulture = new System.Globalization.CultureInfo(GlobalDataHelper<AppConfig>.Config.Lang);
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigHelper.Instance.SetLang(GlobalDataHelper<AppConfig>.Config.Lang);
            Container.Resolve<IRegionManager>().RegisterViewWithRegion("ContentRegion", typeof(Packages));
        }

        private bool IsOSSupported()
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
        protected override System.Windows.Window CreateShell()
        {
            MainWindow shell = Container.Resolve<MainWindow>();
            if (!IsOSSupported())
            {
                HandyControl.Controls.MessageBox.Show(Lang.ResourceManager.GetString("OSNotSupport"));
                Environment.Exit(0);
            }

            if (!Tools.IsWingetInstalled())
            {
                HandyControl.Controls.MessageBox.Show(Lang.ResourceManager.GetString("WingetNotInstalled"), "Install Winget");
                ProcessStartInfo ps = new ProcessStartInfo("https://github.com/microsoft/winget-cli/releases")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
                Environment.Exit(0);
            }

            if (GlobalDataHelper<AppConfig>.Config.Skin != SkinType.Default)
            {
                UpdateSkin(GlobalDataHelper<AppConfig>.Config.Skin);
            }
            return shell;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Packages>();
            containerRegistry.RegisterForNavigation<About>();
            containerRegistry.RegisterForNavigation<Settings>();
            containerRegistry.RegisterForNavigation<Updater>();
            containerRegistry.RegisterForNavigation<UnderConstruction>();
        }
        internal void UpdateSkin(SkinType skin)
        {
            Resources.MergedDictionaries.Add(ResourceHelper.GetSkin(skin));
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
            });
            Current.MainWindow?.OnApplyTemplate();
        }
    }
}
