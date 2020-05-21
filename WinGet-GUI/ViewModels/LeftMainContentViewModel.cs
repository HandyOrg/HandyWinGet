using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows.Controls;

namespace WinGet_GUI.ViewModels
{
    public class LeftMainContentViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        #region Command
        public DelegateCommand<SelectionChangedEventArgs> SwitchItemCmd { get; private set; }
        public DelegateCommand EmptyContentCommand { get; private set; }
        #endregion

        public LeftMainContentViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            SwitchItemCmd = new DelegateCommand<SelectionChangedEventArgs>(Switch);
            EmptyContentCommand = new DelegateCommand(EmptyContent);
        }
        private void EmptyContent()
        {
            _regionManager.Regions["ContentRegion"].RemoveAll();
        }

        private void Switch(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            if (e.AddedItems[0] is ListBoxItem item)
            {
                if (item.Tag != null)
                {
                    _regionManager.RequestNavigate("ContentRegion", item.Tag.ToString());
                }
            }
        }
    }
}
