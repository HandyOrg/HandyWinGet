using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace HandyWinget_GUI
{
    public class Tools
    {
        public static void DeleteDirectory(string d)
        {
            if (System.IO.Directory.Exists(d))
            {
                foreach (string sub in System.IO.Directory.EnumerateDirectories(d))
                {
                    DeleteDirectory(sub);
                }
                foreach (string f in System.IO.Directory.EnumerateFiles(d))
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(f)
                    {
                        Attributes = FileAttributes.Normal
                    };
                    fi.Delete();
                }
                System.IO.Directory.Delete(d);
            }
        }

        public static bool IsWingetInstalled()
        {
            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                proc.Start();
                return true;
            }
            catch (Win32Exception)
            {

                return false;
            }
        }
    }
}
