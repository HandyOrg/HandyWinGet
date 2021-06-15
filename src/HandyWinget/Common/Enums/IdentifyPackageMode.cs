using System.ComponentModel;
using HandyControl.Tools.Converter;

namespace HandyWinget.Common
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum IdentifyPackageMode
    {
        [Description("Off (Fast)")]
        Off,

        [Description("Winget-cli Method")]
        Wingetcli
    }
}
