using System;
using System.IO;

namespace HandyWinget.Assets
{
    public abstract class Consts
    {
        public static string AppName = "HandyWinGet";
        public static string VersionKey = "VersionCode";
        private static string ManifestFolderName = "manifests";
        private static string TempFolderName = "Temp";

        public static string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
        public static string TempSetupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, TempFolderName);
        public static string ManifestPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, ManifestFolderName);
        public static string ConfigPath = Path.Combine(RootPath, "Config.json");

        public static string WingetRepository = "https://github.com/microsoft/winget-cli/releases";
        public static string WingetPkgsRepository = "https://github.com/microsoft/winget-pkgs/archive/master.zip";

        public static string IDManX64Location = @"C:\Program Files (x86)\Internet Download Manager\IDMan.exe";
        public static string IDManX86Location = @"C:\Program Files\Internet Download Manager\IDMan.exe";
    }
}
