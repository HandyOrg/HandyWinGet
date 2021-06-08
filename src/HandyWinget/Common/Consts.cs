using System;
using System.IO;

namespace HandyWinget.Common
{
    public static class Consts
    {
        public static readonly string AppName = "HandyWinGet";
        private static readonly string ManifestFolder = "manifestsV4";
        private static readonly string TempFolder = "Temp";

        public static readonly string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
        public static readonly string TempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, TempFolder);
        public static readonly string ManifestPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, ManifestFolder);
        public static readonly string SettingsPath = Path.Combine(RootPath, "Config.json");
        public static readonly string CachePath = Path.Combine(RootPath, "Cache");
        
        public static readonly string AppSecret = "0153dc1d-eda3-4da2-98c9-ce29361d622d";

        public static readonly string IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
        public static readonly string IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";
    }
}
