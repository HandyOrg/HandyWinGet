using HandyControl.Tools.Converter;
using System.ComponentModel;

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