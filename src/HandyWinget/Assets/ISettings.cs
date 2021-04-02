using HandyControl.Themes;
using HandyWinget.Assets;
using ModernWpf.Controls;
using Nucs.JsonSettings;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandyWinget
{
    public class ISettings : JsonSettings
    {
        public override string FileName { get; set; } = Consts.ConfigPath;

        #region Property

        public virtual int Version { get; set; } = 13991209;
        
        public virtual DateTime UpdatedDate { get; set; } = DateTime.Now;
        public virtual bool IsFirstRun { get; set; } = true;
        public virtual bool IsIDMEnabled { get; set; } = false;
        public virtual bool GroupByPublisher { get; set; } = false;
        public virtual DataGridRowDetailsVisibilityMode ShowExtraDetails { get; set; } = DataGridRowDetailsVisibilityMode.Collapsed;
        public virtual NavigationViewPaneDisplayMode PaneDisplayMode { get; set; } = NavigationViewPaneDisplayMode.Top;
        public virtual InstallMode InstallMode { get; set; } = InstallMode.Internal;
        public virtual IdentifyPackageMode IdentifyPackageMode { get; set; } = IdentifyPackageMode.Off;
        public virtual ApplicationTheme Theme { get; set; } = ApplicationTheme.Light;
        public virtual Brush Accent { get; set; }

        #endregion Property

        public ISettings()
        {
            Version = 13991209;
        }

        public ISettings(string fileName) : base(fileName)
        {
        }
    }
}
