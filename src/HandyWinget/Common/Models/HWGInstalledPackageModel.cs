namespace HandyWinget.Common.Models
{
    public class HWGInstalledPackageModel
    {
        public string PackageId { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string YamlUri { get; set; }
        public string ProductCode { get; set; }
        public string Version { get; set; }
        public string AvailableVersion { get; set; }
    }
}
