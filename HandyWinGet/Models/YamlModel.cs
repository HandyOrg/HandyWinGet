using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyWinGet.Models
{
    public class YamlModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AppMoniker { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string Author { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public string Homepage { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string InstallerType { get; set; }
        public Switches Switches { get; set; }
        public List<Installer> Installers { get; set; }
    }
    public class Switches
    {
        public string Custom { get; set; }
        public string Silent { get; set; }
        public string SilentWithProgress { get; set; }
    }

    public class Installer
    {
        public string Arch { get; set; }
        public string Url { get; set; }
        public string Sha256 { get; set; }
    }

}
