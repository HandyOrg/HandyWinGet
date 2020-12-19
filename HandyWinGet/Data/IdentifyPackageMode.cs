using System.ComponentModel;
using HandyControl.Tools.Converter;

namespace HandyWinGet.Data
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum IdentifyPackageMode
    {
        [Description("Off (Fast)")] Off,

        [Description("Internal Method (Normal)")]
        Internal,

        [Description("Winget-cli Method (Slow)")]
        Wingetcli
    }
}