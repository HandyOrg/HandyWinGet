using HandyControl.Tools.Converter;
using System.ComponentModel;

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
