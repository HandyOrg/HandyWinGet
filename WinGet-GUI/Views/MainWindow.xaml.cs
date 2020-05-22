using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using System.Windows;
using System.Windows.Controls;
using WinGet_GUI.Assets.Languages;

namespace WinGet_GUI.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Change Skin and Language
        private void ButtonConfig_OnClick(object sender, RoutedEventArgs e)
        {
            PopupConfig.IsOpen = true;
        }

        private void ButtonSkins_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button && button.Tag is SkinType tag)
            {
                PopupConfig.IsOpen = false;
                if (tag.Equals(GlobalDataHelper<AppConfig>.Config.Skin))
                {
                    return;
                }

                GlobalDataHelper<AppConfig>.Config.Skin = tag;
                GlobalDataHelper<AppConfig>.Save();
                ((App)System.Windows.Application.Current).UpdateSkin(tag);
            }
        }

        private void ButtonLangs_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button && button.Tag is string tag)
            {
                PopupConfig.IsOpen = false;
                if (tag.Equals(GlobalDataHelper<AppConfig>.Config.Lang))
                {
                    return;
                }

                Growl.Ask(Lang.ResourceManager.GetString("ChangeLanguage"), b =>
                {
                    if (!b)
                    {
                        return true;
                    }

                    GlobalDataHelper<AppConfig>.Config.Lang = tag;
                    GlobalDataHelper<AppConfig>.Save();
                    LocalizationManager.Instance.CurrentCulture = new System.Globalization.CultureInfo(tag);
                    ConfigHelper.Instance.SetLang(GlobalDataHelper<AppConfig>.Config.Lang);
                    return true;
                });
            }
        }
        #endregion
    }
}
