using System;
using System.IO;

namespace HandyWinget.Common
{
    public static class Consts
    {
        /// <summary>
        /// HandyWinGet
        /// </summary>
        public const string AppName = "HandyWinGet";

        /// <summary>
        /// ApplicationData\HandyWinGet
        /// </summary>
        public static readonly string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

        /// <summary>
        /// ApplicationData\HandyWinGet\Temp
        /// </summary>
        public static readonly string TempPath = Path.Combine(RootPath, "Temp");

        /// <summary>
        /// ApplicationData\HandyWinGet\Config.json
        /// </summary>
        public static readonly string SettingsPath = Path.Combine(RootPath, "Config.json");

        /// <summary>
        /// ApplicationData\HandyWinGet\Cache
        /// </summary>
        public static readonly string CachePath = Path.Combine(RootPath, "Cache");
        
        public const string AppSecret = "0153dc1d-eda3-4da2-98c9-ce29361d622d";

        public const string IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
        public const string IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";

        #region Database

        /// <summary>
        /// https://winget.azureedge.net/cache/
        /// </summary>
        public const string AzureBaseUrl = "https://winget.azureedge.net/cache/";

        /// <summary>
        /// https://winget.azureedge.net/cache/source.msix
        /// </summary>
        public const string MSIXSourceUrl = "https://winget.azureedge.net/cache/source.msix";

        /// <summary>
        /// ApplicationData\HandyWinGet\Database
        /// </summary>
        public static readonly string DatabasePath = Path.Combine(RootPath, "Database");


        /// <summary>
        /// ApplicationData\HandyWinGet\Database\MSIX
        /// </summary>
        public static readonly string MSIXPath = Path.Combine(DatabasePath, "MSIX");

        /// <summary>
        /// ApplicationData\HandyWinGet\Database\MSIX\Public\index.db
        /// </summary>
        public static readonly string MSIXDatabasePath = Path.Combine(MSIXPath, "Public", "index.db");

        /// <summary>
        /// ApplicationData\HandyWinGet\Database\indexV4.db
        /// </summary>
        public static readonly string HWGDatabasePath = Path.Combine(DatabasePath, "indexV4.db");

        #endregion
    }
}
