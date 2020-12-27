using System.Diagnostics;
using HandyControl.Controls;
using HandyWinGet.Data;
using System.Windows;
using System.Windows.Controls;
using HandyWinGet.Models;
using HandyWinGet.ViewModels;
using MessageBox = HandyControl.Controls.MessageBox;

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
            dg.RowDetailsVisibilityMode = selectedRows > 1 ? DataGridRowDetailsVisibilityMode.Collapsed : DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
        }

    }
}