using HandyControl.Controls;
using HandyControl.Themes;
using HandyWinget.Assets;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using nucs.JsonSettings;
using nucs.JsonSettings.Autosave;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Media;

namespace HandyWinget
{
    public partial class App : Application
    {
        ISettings Settings = JsonSettings.Load<ISettings>().EnableAutosave();

        public App()
        {
            if (!Directory.Exists(Consts.CachePath))
            {
                Directory.CreateDirectory(Consts.CachePath);
            }

            ProfileOptimization.SetProfileRoot(Consts.CachePath);
            ProfileOptimization.StartProfile("Profile");
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!Settings.Version.Equals(RegistryHelper.GetKey<int>(Consts.VersionKey, Consts.AppName, HKEYType.CurrentUser)))
            {
                if (File.Exists(Consts.ConfigPath))
                {
                    File.Delete(Consts.ConfigPath);
                }
                RegistryHelper.AddOrUpdateKey(Consts.VersionKey, Consts.AppName, Settings.Version, HKEYType.CurrentUser);
            }

            UpdateTheme(Settings.Theme);
            UpdateAccent(Settings.Accent);
            AppCenter.Start(Consts.AppSecret, typeof(Analytics), typeof(Crashes));
        }

        internal void UpdateTheme(ApplicationTheme theme)
        {
            if (ThemeManager.Current.ApplicationTheme != theme)
            {
                ThemeManager.Current.ApplicationTheme = theme;
                ModernWpf.ThemeManager.Current.ApplicationTheme = theme == ApplicationTheme.Light ? (ModernWpf.ApplicationTheme?)ModernWpf.ApplicationTheme.Light : (ModernWpf.ApplicationTheme?)ModernWpf.ApplicationTheme.Dark;
            }
        }

        internal void UpdateAccent(Brush accent)
        {
            if (ThemeManager.Current.AccentColor != accent)
            {
                ThemeManager.Current.AccentColor = accent;
                ModernWpf.ThemeManager.Current.AccentColor = accent == null ? null : (Color?)Helper.GetColorFromBrush(accent);
            }
        }
    }
}
