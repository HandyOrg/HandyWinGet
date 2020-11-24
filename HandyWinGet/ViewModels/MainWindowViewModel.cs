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
        IRegionManager region;
        private DelegateCommand<NavigationViewSelectionChangedEventArgs> _SwitchCommand;
        public DelegateCommand<NavigationViewSelectionChangedEventArgs> SwitchCommand =>
            _SwitchCommand ?? (_SwitchCommand = new DelegateCommand<NavigationViewSelectionChangedEventArgs>(Switch));

        private NavigationViewPaneDisplayMode _paneDisplayMode;
        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get { return GlobalDataHelper<AppConfig>.Config.PaneDisplayMode; }
            set { SetProperty(ref _paneDisplayMode, value); }
        }

        public MainWindowViewModel(IRegionManager regionManager)
        {
            region = regionManager;
        }

        private void Switch(NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item)
            {
                if (item.Tag != null)
                {
                    region.RequestNavigate("ContentRegion", item.Tag.ToString());
                }
            }
        }
    }
}
