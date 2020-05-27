using HandyControl.Controls;
using HandyControl.Data;

namespace HandyWinget_GUI
{
    internal class AppConfig : GlobalDataHelper<AppConfig>
    {
        public string Lang { get; set; } = "en-US";
        public bool IsCheckedCompanyName { get; set; } = true;
        public bool IsCheckAppInstalled { get; set; } = false;

        public SkinType Skin { get; set; } = SkinType.Default;
    }
}