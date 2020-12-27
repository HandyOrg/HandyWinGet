using HandyWinGet.Models;
using System.Collections.Generic;

namespace HandyWinGet.Data
{
    public class ItemEqualityComparer : IEqualityComparer<PackageModel>
    {
        public bool Equals(PackageModel x, PackageModel y)
        {
            return x?.Name == y?.Name;
        }

        public int GetHashCode(PackageModel obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
