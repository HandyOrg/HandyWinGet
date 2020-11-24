namespace HandyWinGet.Models
{
    public class PackageModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Version { get; set; }
        public bool IsInstalled { get; set; }
    }
}
