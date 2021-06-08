using HandyControl.Themes;
using HandyControl.Tools;
using HandyWinget.Common;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Windows;
using System.Windows.Media;
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
