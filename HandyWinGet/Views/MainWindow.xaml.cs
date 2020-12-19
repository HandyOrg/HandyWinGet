using HandyControl.Controls;
using HandyControl.Data;
using HandyWinGet.Data;
using System.Windows;

namespace HandyWinGet.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            if (GlobalDataHelper<AppConfig>.Config.Skin.Equals(SkinType.Default))
            {
                tg.IsChecked = false;
            }
            else
            {
                tg.IsChecked = true;
            }
        }

        private void ToggleSkins_OnClick(object sender, RoutedEventArgs e)
        {
            var tag = SkinType.Default;
            if (tg.IsChecked.Value)
            {
                tag = SkinType.Dark;
            }
            else
            {
                tag = SkinType.Default;
            }

            if (tag.Equals(GlobalDataHelper<AppConfig>.Config.Skin))
            {
                return;
            }

            GlobalDataHelper<AppConfig>.Config.Skin = tag;
            GlobalDataHelper<AppConfig>.Save();
            ((App) System.Windows.Application.Current).UpdateSkin(tag);
        }
    }
}