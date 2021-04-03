using System.Collections.Generic;

namespace HandyWinget.Assets
{
    public class VersionModel
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public List<Installer> Installers { get; set; }
    }
}