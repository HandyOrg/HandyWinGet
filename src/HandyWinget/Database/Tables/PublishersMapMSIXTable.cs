using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HandyWinget.Database.Tables
{
    [Table("norm_publishers_map")]
    [Keyless]
    public class PublishersMapMSIXTable
    {
        public long manifest { get; set; }
        public long norm_publisher { get; set; }

    }
}
