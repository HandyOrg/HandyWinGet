using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using HandyWinget_GUI.Assets;
using HandyWinget_GUI.Assets.Languages;
using HandyWinget_GUI.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Diagnostics;
using System.Windows;

namespace HandyWinget_GUI
{
    public partial class App
    {
        public App()
        {
            AppCenter.Start("0153dc1d-eda3-4da2-98c9-ce29361d622d",
                   typeof(Analytics), typeof(Crashes));

            GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
            LocalizationManager.Instance.LocalizationProvider = new ResxProvider();
            LocalizationManager.Instance.CurrentCulture = new System.Globalization.CultureInfo(GlobalDataHelper<AppConfig>.Config.Lang);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigHelper.Instance.SetLang(GlobalDataHelper<AppConfig>.Config.Lang);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Container.Resolve<IRegionManager>().RegisterViewWithRegion("ContentRegion", typeof(Packages));
        }


        protected override System.Windows.Window CreateShell()
        {
            MainWindow shell = Container.Resolve<MainWindow>();
            if (!Tools.IsOSSupported())
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
            containerRegistry.RegisterForNavigation<CreatePackages>();
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
