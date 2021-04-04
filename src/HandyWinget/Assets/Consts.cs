using System;
using System.IO;

namespace HandyWinget.Assets
{
    public static class Consts
    {
        public static readonly string AppName = "HandyWinGet";
        public static readonly string VersionKey = "VersionCode";
        private static readonly string ManifestFolderName = "manifestsV1";
        private static readonly string TempFolderName = "Temp";

        public static readonly string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
        public static readonly string TempSetupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, TempFolderName);
        public static readonly string ManifestPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, ManifestFolderName);
        public static readonly string ConfigPath = Path.Combine(RootPath, "Config.json");
        public static readonly string CachePath = Path.Combine(RootPath, "Cache");
        
        public static readonly string AppSecret = "0153dc1d-eda3-4da2-98c9-ce29361d622d";

        public static readonly string WingetRepository = "https://github.com/microsoft/winget-cli/releases";
        public static readonly string WingetPkgsRepository = "https://github.com/microsoft/winget-pkgs/archive/master.zip";

        public static readonly string IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
        public static readonly string IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";
    }
}
