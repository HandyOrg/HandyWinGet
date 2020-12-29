using HandyControl.Controls;
using HandyControl.Data;
using HandyWinGet.Data;
using System.Windows;
namespace HandyWinGet.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public MainWindow()
        {
            InitializeComponent();
            tg.IsChecked = !GlobalDataHelper<AppConfig>.Config.Skin.Equals(SkinType.Default);
        }

        private void ToggleSkins_OnClick(object sender, RoutedEventArgs e)
        {
            var tag = SkinType.Default;
            tag = tg.IsChecked.Value ? SkinType.Dark : SkinType.Default;

            if (tag.Equals(GlobalDataHelper<AppConfig>.Config.Skin))
            {
                return;
            }

            GlobalDataHelper<AppConfig>.Config.Skin = tag;
            GlobalDataHelper<AppConfig>.Save();
            ((App)Application.Current).UpdateSkin(tag);
        }
    }
}