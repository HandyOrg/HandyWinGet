using System.Windows.Controls;

namespace WinGet_GUI.Views
{
    /// <summary>
    /// Interaction logic for About
    /// </summary>
    public partial class About : UserControl
    {
        public About()
        {
            InitializeComponent();
        }

        private void SearchBar_SearchStarted(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {

        }
    }
}
