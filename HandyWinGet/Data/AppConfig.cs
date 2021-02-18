using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Themes;
using ModernWpf.Controls;
using System;

namespace HandyWinGet.Data
{
    internal class AppConfig : GlobalDataHelper<AppConfig>
    {
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public bool IsIDMEnabled { get; set; } = false;
        public bool IsShowingGroup { get; set; } = false;
        public bool IsShowingExtraDetail { get; set; } = false;
        public NavigationViewPaneDisplayMode PaneDisplayMode { get; set; } = NavigationViewPaneDisplayMode.Left;
        public InstallMode InstallMode { get; set; } = InstallMode.Wingetcli;
        public IdentifyPackageMode IdentifyPackageMode { get; set; } = IdentifyPackageMode.Off;
        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Light;
    }
}