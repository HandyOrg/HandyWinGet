using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;

namespace HandyWinget_GUI.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private bool _IsCheckedCompanyName;
        public bool IsCheckedCompanyName
        {
            get => _IsCheckedCompanyName;
            set => SetProperty(ref _IsCheckedCompanyName, value);
        }

        public DelegateCommand<object> IsCheckedCompanyNameCommand { get; private set; }

        public SettingsViewModel()
        {
            IsCheckedCompanyNameCommand = new DelegateCommand<object>(OnCompanyNameChecked);
            InitSettings();
        }

        private void OnCompanyNameChecked(object isChecked)
        {
            if ((bool)isChecked != GlobalDataHelper<AppConfig>.Config.IsCheckedCompanyName)
            {
                GlobalDataHelper<AppConfig>.Config.IsCheckedCompanyName = (bool)isChecked;
                GlobalDataHelper<AppConfig>.Save();
            }
        }
        private void InitSettings()
        {
            IsCheckedCompanyName = GlobalDataHelper<AppConfig>.Config.IsCheckedCompanyName;
        }
    }
}
