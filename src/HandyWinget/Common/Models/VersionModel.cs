using System.Collections.Generic;

namespace HandyWinget.Common
{
    public class VersionModel
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public List<Installer> Installers { get; set; }
    }
}