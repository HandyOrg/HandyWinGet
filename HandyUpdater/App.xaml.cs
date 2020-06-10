using System;
using System.Windows;

namespace HandyUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static UpdateModel Argument = new UpdateModel();

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                Argument = new UpdateModel
                {
                    CurrentVersion = e.Args[0],
                    NewVersion = e.Args[1],
                    Location = e.Args[2],
                    ExeLocation = e.Args[3],
                    Url = e.Args[4]
                };
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
