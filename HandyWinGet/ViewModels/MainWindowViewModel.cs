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
        private readonly IRegionManager region;

        private NavigationViewPaneDisplayMode _paneDisplayMode;
        private DelegateCommand<NavigationViewSelectionChangedEventArgs> _SwitchCommand;

        public MainWindowViewModel(IRegionManager regionManager)
        {
            Instance = this;
            region = regionManager;
        }

        public DelegateCommand<NavigationViewSelectionChangedEventArgs> SwitchCommand =>
            _SwitchCommand ?? (_SwitchCommand = new DelegateCommand<NavigationViewSelectionChangedEventArgs>(Switch));

        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get => GlobalDataHelper<AppConfig>.Config.PaneDisplayMode;
            set => SetProperty(ref _paneDisplayMode, value);
        }

        private void Switch(NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item)
                if (item.Tag != null)
                    region.RequestNavigate("ContentRegion", item.Tag.ToString());
        }
    }
}