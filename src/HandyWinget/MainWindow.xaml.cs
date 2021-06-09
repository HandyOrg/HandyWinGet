using ModernWpf.Controls;
using Prism.Regions;
using static HandyWinget.Common.Helper;
namespace HandyWinget
{
    public partial class MainWindow
    {
        internal static MainWindow Instance;

        IRegionManager _regionManager;
        public MainWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            _regionManager = regionManager;
            Instance = this;

            LoadSettings();
        }

        private void LoadSettings()
        {
            if (Settings.IsFirstRun)
            {
                navView.SelectedItem = navView.MenuItems[0];
                Settings.IsFirstRun = false;
            }

            navView.PaneDisplayMode = Settings.PaneDisplayMode;
        }
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem) args.SelectedItem;
            if (selectedItem != null)
            {
                Navigate(selectedItem.Tag.ToString());
            }
        }

        private void Navigate(string view)
        {
            _regionManager.RequestNavigate("ContentRegion", view);
        }
    }
}
