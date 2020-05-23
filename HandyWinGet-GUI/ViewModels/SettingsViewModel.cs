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

        private bool _IsCheckAppInstalled;
        public bool IsCheckAppInstalled
        {
            get => _IsCheckAppInstalled;
            set => SetProperty(ref _IsCheckAppInstalled, value);
        }

        public DelegateCommand<object> IsCheckedCompanyNameCommand { get; private set; }
        public DelegateCommand<object> IsCheckAppInstalledCommand { get; private set; }

        public SettingsViewModel()
        {
            IsCheckedCompanyNameCommand = new DelegateCommand<object>(OnCompanyNameChecked);
            IsCheckAppInstalledCommand = new DelegateCommand<object>(OnAppInstalledChecked);
            InitSettings();
        }

        private void OnAppInstalledChecked(object isChecked)
        {
            if ((bool)isChecked != GlobalDataHelper<AppConfig>.Config.IsCheckAppInstalled)
            {
                GlobalDataHelper<AppConfig>.Config.IsCheckAppInstalled = (bool)isChecked;
                GlobalDataHelper<AppConfig>.Save();
            }
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
            IsCheckAppInstalled = GlobalDataHelper<AppConfig>.Config.IsCheckAppInstalled;
        }
    }
}
