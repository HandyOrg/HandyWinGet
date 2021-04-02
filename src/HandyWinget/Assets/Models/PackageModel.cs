using System.Collections.Generic;

namespace HandyWinget.Assets
{
    public class PackageModel
    {
        public string PackageIdentifier { get; set; }
        public string PackageName { get; set; }
        public string Publisher { get; set; }
        public string PackageVersion { get; set; }
        public List<string> Versions { get; set; }
        public string Description { get; set; }
        public string LicenseUrl { get; set; }
        public string Homepage { get; set; }
        public List<Installer> Installers { get; set; }
        public string InstalledVersion { get; set; }
        public bool IsInstalled { get; set; }
    }
}