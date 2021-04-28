using HandyControl.Themes;
using HandyControl.Tools;
using HandyWinget.Assets;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Autosave;
using System.IO;
using System.Windows;
using System.Windows.Media;
using static HandyWinget.Assets.Helper;
namespace HandyWinget
{
    public partial class App : Application
    {
        public App()
        {
            ApplicationHelper.StartProfileOptimization();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!Settings.Version.Equals(RegistryHelper.GetValue<int>(Consts.VersionKey, Consts.AppName)))
            {
                if (File.Exists(Consts.ConfigPath))
                {
                    File.Delete(Consts.ConfigPath);
                }
                RegistryHelper.AddOrUpdateKey(Consts.VersionKey, Consts.AppName, Settings.Version);
                Settings = JsonSettings.Load<ISettings>().EnableAutosave();
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
                ModernWpf.ThemeManager.Current.AccentColor = accent == null ? null : ColorHelper.GetColorFromBrush(accent);
            }
        }
    }
}
