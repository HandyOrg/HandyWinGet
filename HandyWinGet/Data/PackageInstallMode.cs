using HandyControl.Tools.Converter;
using System.ComponentModel;

namespace HandyWinGet.Data
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PackageInstallMode
    {
        [Description("Winget-cli")]
        Wingetcli,
        [Description("Internal (Manual installation)")]
        Internal
    }
}
