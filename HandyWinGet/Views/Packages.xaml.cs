using HandyControl.Controls;
using HandyWinGet.Data;
using System.Windows;
using System.Windows.Controls;
using HandyWinGet.Models;

namespace HandyWinGet.Views
{
    /// <summary>
    ///     Interaction logic for Packages
    /// </summary>
    public partial class Packages : UserControl
    {
        internal static Packages Instance;

        public Packages()
        {
            InitializeComponent();
            Instance = this;
            SetPublisherVisibility();
        }

        public void SetPublisherVisibility()
        {
            dg.Columns[0].Visibility = GlobalDataHelper<AppConfig>.Config.IsShowingGroup
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void Dg_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRows = dg.SelectedItems.Count;
            if (selectedRows > 1)
            {
                dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
                mnuCmd.IsEnabled = false;
                mnuUninstall.IsEnabled = false;
            }
            else
            {
                dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
                mnuCmd.IsEnabled = true;
                mnuUninstall.IsEnabled = true;
            }

            if (((PackageModel)dg.SelectedItem).IsInstalled && selectedRows == 1)
            {
                mnuUninstall.IsEnabled = true;
            }
            else
            {
                mnuUninstall.IsEnabled = false;
            }
        }
    }
}