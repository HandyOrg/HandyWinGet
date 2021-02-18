using HandyControl.Controls;
using HandyWinGet.Data;
using ModernWpf.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace HandyWinGet.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        internal static MainWindowViewModel Instance;
        private readonly IRegionManager _region;

        private NavigationViewPaneDisplayMode _paneDisplayMode;
        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get => GlobalDataHelper<AppConfig>.Config.PaneDisplayMode;
            set => SetProperty(ref _paneDisplayMode, value);
        }

        private DelegateCommand<NavigationViewSelectionChangedEventArgs> _switchCommand;
        public DelegateCommand<NavigationViewSelectionChangedEventArgs> SwitchCommand =>
            _switchCommand ??= new DelegateCommand<NavigationViewSelectionChangedEventArgs>(Switch);

        public MainWindowViewModel(IRegionManager regionManager)
        {
            Instance = this;
            _region = regionManager;
        }

        private void Switch(NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item)
            {
                if (item.Tag != null)
                {
                    _region.RequestNavigate("ContentRegion", item.Tag.ToString());
                }
            }
        }
    }
}