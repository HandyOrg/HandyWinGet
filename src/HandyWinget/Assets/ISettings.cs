using HandyControl.Themes;
using HandyWinget.Assets;
using ModernWpf.Controls;
using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandyWinget
{
    public class ISettings : NotifiyingJsonSettings, IVersionable
    {
        public override string FileName { get; set; } = Consts.ConfigPath;

        #region Property

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
        public virtual bool AutoRefreshInStartup { get; set; } = false;
        public virtual bool IsStoreDataGridColumnWidth { get; set; } = false;
        public virtual Version Version { get; set; }

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
        #endregion Property
    }
}
