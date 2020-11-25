using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Themes;
using HandyControl.Tools;
using HandyWinGet.Data;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using ModernWpf;
using System;
using System.IO;
using System.Runtime;
using System.Windows;

namespace HandyWinGet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var cachePath = $"{AppDomain.CurrentDomain.BaseDirectory}Cache";
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            ProfileOptimization.SetProfileRoot(cachePath);
            ProfileOptimization.StartProfile("Profile");

            AppCenter.Start("0153dc1d-eda3-4da2-98c9-ce29361d622d",
                   typeof(Analytics), typeof(Crashes));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
            if (GlobalDataHelper<AppConfig>.Config.Skin != SkinType.Default)
            {
                UpdateSkin(GlobalDataHelper<AppConfig>.Config.Skin);
            }
            var boot = new Bootstrapper();
            boot.Run();
        }
        public void UpdateSkin(SkinType skin)
        {
            SharedResourceDictionary.SharedDictionaries.Clear();
            ResourceHelper.GetTheme("hcTheme", Resources).Skin = skin;
            ThemeManager tm = ThemeManager.Current;

            if (skin == SkinType.Dark)
            {
                tm.ApplicationTheme = ApplicationTheme.Dark;
            }
            else
            {
                tm.ApplicationTheme = ApplicationTheme.Light;
            }

            Current.MainWindow?.OnApplyTemplate();
        }
    }
}
