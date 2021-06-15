using HandyWinget.Common;
using HandyWinget.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace HandyWinget.Database
{
    public class HWGContext : DbContext
    {
        public HWGContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
        {
            optionbuilder.UseSqlite($@"Data Source={Consts.HWGDatabasePath}");
        }

        public DbSet<ManifestTable> ManifestTable { get; set; }
    }
}
