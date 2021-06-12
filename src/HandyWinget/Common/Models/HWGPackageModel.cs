using System.Collections.Generic;

namespace HandyWinget.Common.Models
{
    public class HWGPackageModel
    {
        public string PackageId { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string YamlUri { get; set; }
        public string ProductCode { get; set; }

        public PackageVersion PackageVersion { get; set; }
        public List<PackageVersion> Versions { get; set; }
    }

    public class PackageVersion
    {
        public string Version { get; set; }
        public string YamlUri { get; set; }
    }
}
