using System.Collections.Generic;

namespace HandyWinget.Assets.Models.Export
{
    public class ExportVersionModel
    {
        public string PackageIdentifier { get; set; }
        public string PackageVersion { get; set; }
        public string DefaultLocale { get; set; }
        public string ManifestType { get; set; }
        public string ManifestVersion { get; set; }
        public string PackageName { get; set; }
        public string Publisher { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public string PackageUrl { get; set; }
        public string ShortDescription { get; set; }
    }
}
