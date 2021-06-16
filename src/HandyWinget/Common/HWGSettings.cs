using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Themes;
using HandyWinget.Common;
using ModernWpf.Controls;
using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace HandyWinget
{
    public class HWGSettings : NotifiyingJsonSettings, IVersionable
    {
        public override string FileName { get; set; } = Consts.SettingsPath;

        [EnforcedVersion("4.0.0.0")]
        public virtual Version Version { get; set; } = new Version(1, 0, 0, 0);

        #region Property
        public virtual bool IsFirstRun { get; set; } = true;
        public virtual bool IsIDMEnabled { get; set; } = false;
        public virtual bool GroupByPublisher { get; set; } = false;
        public virtual bool AutoRefreshInStartup { get; set; } = false;
        public virtual bool IsStoreDataGridColumnWidth { get; set; } = false;
        public virtual bool IsShowNotifications { get; set; } = true;
        public virtual bool IdentifyInstalledPackage { get; set; } = false;
        public virtual bool AutoDownloadPackage { get; set; } = false;
        public virtual DateTime UpdatedDate { get; set; } = DateTime.Now;
        public virtual NavigationViewPaneDisplayMode PaneDisplayMode { get; set; } = NavigationViewPaneDisplayMode.Top;
        public virtual InstallMode InstallMode { get; set; } = InstallMode.Internal;
        public virtual ApplicationTheme Theme { get; set; } = ApplicationTheme.Light;
        public virtual Brush Accent { get; set; }

        private ObservableCollection<DataGridLength> _DataGridColumnWidth = new ObservableCollection<DataGridLength>();
        public virtual ObservableCollection<DataGridLength> DataGridColumnWidth
        {
            get => _DataGridColumnWidth;
            set
            {
                if (Equals(value, _DataGridColumnWidth)) return;
                _DataGridColumnWidth = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DataGridLength> _DataGridInstalledColumnWidth = new ObservableCollection<DataGridLength>();
        public virtual ObservableCollection<DataGridLength> DataGridInstalledColumnWidth
        {
            get => _DataGridInstalledColumnWidth;
            set
            {
                if (Equals(value, _DataGridInstalledColumnWidth)) return;
                _DataGridInstalledColumnWidth = value;
                OnPropertyChanged();
            }
        }
        #endregion Property
    }
}
