using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HandyWinget.Database.Tables
{
    [Table("metadata")]
    [Keyless]
	public class MetadataMSIXTable
	{
		public string name { get; set; }
		public string value { get; set; }
		
	}
}
