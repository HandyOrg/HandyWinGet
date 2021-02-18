using HandyControl.Controls;
using HandyWinGet.Data;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.IO;
using System.Runtime;
using System.Windows;
using ApplicationTheme = HandyControl.Themes.ApplicationTheme;

namespace HandyWinGet
{
    /// <summary>
    ///     Interaction logic for App.xaml
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
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GlobalDataHelper<AppConfig>.Init($"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json");
            if (GlobalDataHelper<AppConfig>.Config.Theme != ApplicationTheme.Light)
            {
                UpdateSkin(GlobalDataHelper<AppConfig>.Config.Theme);
            }

            var boot = new Bootstrapper();
            boot.Run();

            AppCenter.Start("0153dc1d-eda3-4da2-98c9-ce29361d622d",
                typeof(Analytics), typeof(Crashes));
        }

        public void UpdateSkin(ApplicationTheme theme)
        {
            HandyControl.Themes.ThemeManager.Current.ApplicationTheme = theme;

            ModernWpf.ThemeManager.Current.ApplicationTheme = theme == ApplicationTheme.Dark ? ModernWpf.ApplicationTheme.Dark : ModernWpf.ApplicationTheme.Light;
        }
    }
}