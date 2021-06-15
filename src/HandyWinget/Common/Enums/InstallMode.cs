using System.ComponentModel;
using HandyControl.Tools.Converter;

namespace HandyWinget.Common
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum InstallMode
    {
        [Description("Winget-cli")]
        Wingetcli,

        [Description("Internal (Manual installation)")]
        Internal
    }
}