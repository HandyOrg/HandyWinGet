using HandyControl.Controls;
using HandyWinGet.Data;
using System.Windows;
using System.Windows.Controls;

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
    }
}