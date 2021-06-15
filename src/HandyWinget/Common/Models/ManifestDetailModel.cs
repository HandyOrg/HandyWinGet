using System.Collections.Generic;

namespace HandyWinget.Common.Models
{
    public class ManifestDetailModel
    {
        public string PackageIdentifier { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string Publisher { get; set; }
        public string License { get; set; }
        public string ShortDescription { get; set; }
        public List<Installer> Installers { get; set; }
    }
}
