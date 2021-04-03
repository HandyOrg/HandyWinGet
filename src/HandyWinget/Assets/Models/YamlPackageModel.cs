using System.Collections.Generic;

namespace HandyWinget.Assets
{
    public class YamlPackageModel
    {
        public string PackageIdentifier { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string Publisher { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public string PackageUrl { get; set; }
        public string ShortDescription { get; set; }
        public string ManifestType { get; set; }
        public string ManifestVersion { get; set; }
        public string PackageLocale { get; set; }
        public List<Installer> Installers { get; set; }
    }

    public class Installer
    {
        public string Architecture { get; set; }
        public string InstallerUrl { get; set; }
        public string InstallerSha256 { get; set; }
    }
}
