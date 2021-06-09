using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HandyWinget.Database.Tables
{
    [Table("productcodes_map")]
    [Keyless]
	public class ProductCodesMapMSIXTable
	{
		public long manifest { get; set; }
		public long productcode { get; set; }
	}
}
