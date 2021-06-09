using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("pathparts")]
    public class PathPartsMSIXTable
	{
		[Key]
		public long rowid { get; set; }
		public long parent { get; set; }
		public string pathpart { get; set; }

	}
}
