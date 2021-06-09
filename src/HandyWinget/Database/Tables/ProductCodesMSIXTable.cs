using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("productcodes")]
    public class ProductCodesMSIXTable
	{
		[Key]
		public long rowid { get; set; }
		public string productcode { get; set; }

	}
}
