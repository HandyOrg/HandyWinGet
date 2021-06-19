using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("manifests")]
    public class ManifestTable
    {
        [Key]
        public long Id { get; set; }
        public string PackageId { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Version { get; set; }
        public string YamlUri { get; set; }
        public string ProductCode { get; set; }
    }
}
