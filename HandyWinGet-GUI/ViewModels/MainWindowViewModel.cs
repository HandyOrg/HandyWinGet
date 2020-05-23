using Prism.Mvvm;
namespace HandyWinget_GUI.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "WinGet GUI";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public MainWindowViewModel()
        {

        }
    }
}
