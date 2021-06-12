using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HandyWinget.Database.Tables;
using HandyWinget.Database;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using HandyWinget.Common.Models;

namespace HandyWinget.Common
{
    public class DatabaseOperation
    {
        /// <summary>
        /// Extract information from index.db and import it into the hwg database
        /// </summary>
        public static async void GenerateDatabaseAsync()
        {
            using var msixDB = new MSIXContext();

            using var hwgDB = new HWGContext();

            await hwgDB.Database.EnsureDeletedAsync();
            await hwgDB.Database.EnsureCreatedAsync();

            using var db = msixDB.CreateLinqToDbConnection();

            var pathCte = db.GetCte<PathPartCte>(cte => (
                    from pathPart in msixDB.PathPartsMSIXTable
                    select new PathPartCte
                    {
                        rowid = pathPart.rowid,
                        parent = pathPart.parent,
                        path = pathPart.pathpart
                    }
                )
                .Concat(
                    from pathPart in msixDB.PathPartsMSIXTable
                    from child in cte.Where(child => child.parent == pathPart.rowid)
                    select new PathPartCte
                    {
                        rowid = child.rowid,
                        parent = pathPart.parent,
                        path = pathPart.pathpart + "/" + child.path
                    }
                )
            );

            var query =
                from item in msixDB.IdsMSIXTable
                from manifest in msixDB.Set<ManifestMSIXTable>().Where(e => e.id == item.rowid)

                from yml in msixDB.PathPartsMSIXTable.Where(e => e.rowid == manifest.pathpart)
                from pathPartVersion in msixDB.PathPartsMSIXTable.Where(e => e.rowid == yml.parent)
                from pathPartAppName in msixDB.PathPartsMSIXTable.Where(e => e.rowid == pathPartVersion.parent)
                from pathPartPublisher in msixDB.PathPartsMSIXTable.Where(e => e.rowid == pathPartAppName.parent)
                from pathPart in pathCte.Where(e => e.rowid == pathPartPublisher.parent && e.parent == null)
                from productMap in msixDB.ProductCodesMapMSIXTable.Where(e => e.manifest == manifest.rowid).DefaultIfEmpty()
                from prdCode in msixDB.ProductCodesMSIXTable.Where(e => e.rowid == productMap.productcode).DefaultIfEmpty()
                from version in msixDB.VersionsMSIXTable.Where(e => e.rowid == manifest.version)
                select new ManifestTable
                {
                    PackageId = item.id,
                    ProductCode = prdCode.productcode,
                    YamlUri = $@"{pathPart.path}/{pathPartPublisher.pathpart}/{pathPartAppName.pathpart}/{pathPartVersion.pathpart}/{yml.pathpart}",
                    Version = version.version
                };

            var data = await query.ToArrayAsyncLinqToDB();
            hwgDB.AddRange(data);
            await hwgDB.SaveChangesAsync();
        }

        public async static Task<IEnumerable<HWGPackageModel>> GetAllPackageAsync()
        {
            using var hwgDB = new HWGContext();

            var dbQuery = hwgDB.ManifestTable.Select(x => new
            {
                x.PackageId,
                x.YamlUri,
                x.ProductCode,
                x.Version,
            });
            var data = await dbQuery.ToListAsync();

            return data
                .GroupBy(x => x.PackageId)
                .OrderBy(x => x.Key)
                .Select(g => new HWGPackageModel
                {
                    PackageId = g.Key,
                    Name = g.Select(x => Helper.AddSpacesToString(Helper.GetPublisherAndName(x.YamlUri).name)).First(),
                    Publisher = g.Select(x => Helper.GetPublisherAndName(x.YamlUri).publisher).First(),
                    YamlUri = g.Select(x => x.YamlUri).First(),
                    ProductCode = g.Select(x => x.ProductCode).First(),
                    PackageVersion = g.Select(x => new PackageVersion { Version = x.Version, YamlUri = x.YamlUri }).OrderByDescending(x => x.Version).First(),
                    Versions = g.Select(x => new PackageVersion { Version = x.Version, YamlUri = x.YamlUri }).OrderByDescending(x => x.Version).ToList(),
                });
        }
    }
}
