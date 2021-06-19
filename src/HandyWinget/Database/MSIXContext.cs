using HandyWinget.Common;
using HandyWinget.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace HandyWinget.Database
{
    public class MSIXContext : DbContext
    {
        public MSIXContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
        {
            optionbuilder.UseSqlite($@"Data Source={Consts.MSIXDatabasePath}");
        }

        public DbSet<IdsMSIXTable> IdsMSIXTable { get; set; }
        public DbSet<ManifestMSIXTable> ManifestMSIXTable { get; set; }
        public DbSet<MetadataMSIXTable> MetadataMSIXTable { get; set; }
        public DbSet<PublishersMSIXTable> PublishersMSIXTable { get; set; }
        public DbSet<PublishersMapMSIXTable> PublishersMapMSIXTable { get; set; }
        public DbSet<PathPartsMSIXTable> PathPartsMSIXTable { get; set; }
        public DbSet<ProductCodesMSIXTable> ProductCodesMSIXTable { get; set; }
        public DbSet<ProductCodesMapMSIXTable> ProductCodesMapMSIXTable { get; set; }
        public DbSet<VersionsMSIXTable> VersionsMSIXTable { get; set; }
        public DbSet<NameTable> NameTable { get; set; }
    }
}
