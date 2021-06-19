using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("names")]
    public class NameTable
    {
        [Key]
        public long rowid { get; set; }
        public string name { get; set; }
    }
}
