using System.Collections.Generic;

namespace HandyWinget.Common
{
    public class PackageModel
    {
        public string PackageIdentifier { get; set; }
        public string PackageName { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public string LicenseUrl { get; set; }
        public string Homepage { get; set; }
        public string InstalledVersion { get; set; }
        public bool IsInstalled { get; set; }

        public List<VersionModel> Versions { get; set; }
        public VersionModel PackageVersion { get; set; }
        public Installer PackageArchitecture { get; set; }

    }
}