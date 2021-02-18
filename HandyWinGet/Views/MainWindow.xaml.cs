using HandyControl.Controls;
using HandyControl.Themes;
using HandyWinGet.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        }

        private void MenuSkins_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem button && button.Tag is ApplicationTheme tag)
            {
                if (tag.Equals(GlobalDataHelper<AppConfig>.Config.Theme)) return;

                GlobalDataHelper<AppConfig>.Config.Theme = tag;
                GlobalDataHelper<AppConfig>.Save();
                ((App)Application.Current).UpdateSkin(tag);
            }
        }

        private void MenuTerminal_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem button && button.Header is string tag)
            {
                OpenTerminal(tag);
            }
        }

        private void OpenTerminal(string tag)
        {
            switch (tag)
            {
                case "Powershell":
                    System.Diagnostics.Process.Start("powershell.exe");
                    break;
                case "CMD":
                    System.Diagnostics.Process.Start("cmd.exe");
                    break;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                OpenTerminal("Powershell");
            }
            else if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                OpenTerminal("CMD");
            }
        }
    }
}