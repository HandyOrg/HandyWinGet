using System.ComponentModel;
using HandyControl.Tools.Converter;

namespace HandyWinGet.Data
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum InstallMode
    {
        [Description("Winget-cli")] Wingetcli,

        [Description("Internal (Manual installation)")]
        Internal
    }
}