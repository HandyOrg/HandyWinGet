using System.Windows;
using System.Windows.Media;
using HandyControl.Themes;
using HandyControl.Tools;
using HandyWinget.Common;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using static HandyWinget.Common.Helper;
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
            var boot = new Bootstrapper();
            boot.Run();
            UpdateTheme(Settings.Theme);
            UpdateAccent(Settings.Accent);
            ConfigHelper.Instance.SetLang("en");
            AppCenter.Start(Consts.AppSecret, typeof(Analytics), typeof(Crashes));
        }

        internal void UpdateTheme(ApplicationTheme theme)
        {
            ThemeManager.Current.ApplicationTheme = theme;
            ModernWpf.ThemeManager.Current.ApplicationTheme = theme == ApplicationTheme.Light ? ModernWpf.ApplicationTheme.Light : ModernWpf.ApplicationTheme.Dark;
        }

        internal void UpdateAccent(Brush accent)
        {
            ThemeManager.Current.AccentColor = accent;
            ModernWpf.ThemeManager.Current.AccentColor = accent == null ? null : ColorHelper.GetColorFromBrush(accent);
        }
    }
}
