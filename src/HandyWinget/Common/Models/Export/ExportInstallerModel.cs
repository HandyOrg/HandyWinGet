using System.Collections.Generic;

namespace HandyWinget.Common.Models.Export
{
    public class ExportInstallerModel
    {
        public string PackageIdentifier { get; set; }
        public string PackageVersion { get; set; }
        public List<Installer> Installers { get; set; }
        public string ManifestType { get; set; }
        public string ManifestVersion { get; set; }
    }
}
