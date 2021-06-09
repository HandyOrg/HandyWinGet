using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("ids")]
    public class IdsMSIXTable
	{
		[Key]
		public long rowid { get; set; }
		public string id { get; set; }
	}
}
