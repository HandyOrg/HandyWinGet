using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Windows;
using WinGet_GUI.Assets;
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
        protected override System.Windows.Window CreateShell()
        {
            MainWindow shell = Container.Resolve<MainWindow>();
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
