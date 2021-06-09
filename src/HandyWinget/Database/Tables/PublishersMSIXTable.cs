using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyWinget.Database.Tables
{
    [Table("norm_publishers")]
    public class PublishersMSIXTable
	{
		[Key]
		public long rowid { get; set; }
		public string norm_publisher { get; set; }
	}
}
