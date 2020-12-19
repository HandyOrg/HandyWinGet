using HandyControl.Tools.Converter;
using System.ComponentModel;

namespace HandyWinGet.Data
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum IdentifyPackageMode
    {
        [Description("Off (Fast)")]
        Off,
        [Description("Internal Method (Normal)")]
        Internal,
        [Description("Winget-cli Method (Slow)")]
        Wingetcli
    }
}
