using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("versions")]
    public class VersionsMSIXTable
	{
		[Key]
		public long rowid { get; set; }
		public string version { get; set; }
	}
}
