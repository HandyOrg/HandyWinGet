using HandyControl.Controls;
using HandyControl.Data;
using System;

namespace WinGet_GUI
{
    internal class AppConfig : GlobalDataHelper<AppConfig>
    {
        public static readonly string SavePath = $"{AppDomain.CurrentDomain.BaseDirectory}AppConfig.json";

        public string Lang { get; set; } = "en-US";

        public SkinType Skin { get; set; } = SkinType.Default;
    }
}