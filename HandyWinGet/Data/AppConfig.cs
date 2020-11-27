using HandyControl.Controls;
using HandyControl.Data;
using ModernWpf.Controls;
using System;

namespace HandyWinGet.Data
{
    internal class AppConfig : GlobalDataHelper<AppConfig>
    {
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public bool IsCheckedCompanyName { get; set; } = true;
        public bool IsIDM { get; set; } = false;
        public NavigationViewPaneDisplayMode PaneDisplayMode { get; set; } = NavigationViewPaneDisplayMode.Left;
        public PackageInstallMode PackageInstallMode { get; set; } = PackageInstallMode.Wingetcli;
        public SkinType Skin { get; set; } = SkinType.Default;
    }
}
