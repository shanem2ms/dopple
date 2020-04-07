using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Planes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Dopple.Recording Recording;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                byte []bytes = System.IO.File.ReadAllBytes(e.Args[0]);
                App.Recording = new Dopple.Recording(
                    e.Args[0],
                    bytes,
                    new Dopple.Settings() { });
            }
            MainWindow mw = new MainWindow();
            mw.Show();
        }
    }
}
