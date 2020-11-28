namespace HandyWinGet.Models
{
    public class PackageModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string LicenseUrl { get; set; }
        public string Homepage { get; set; }
        public string Arch { get; set; }
        public bool IsInstalled { get; set; }
    }
}
