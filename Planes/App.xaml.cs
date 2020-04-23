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
        public static OpenCV OpenCV;
        public static PtCloudAligner ptCloudAligner;
        public static int FrameDelta = 1;
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
            //PtCloudAligner.Test();
            OpenCV = new OpenCV();
            ptCloudAligner = new PtCloudAligner();
            MainWindow mw = new MainWindow();
            mw.Show();
        }

        public static Settings Settings = new Settings();
    }

    public class Settings
    {
        float planeMinSize = 1.0f;
        float planeThreshold = 0.045f;
        float minDPVal = 0.9f;

        public float PlaneMinSize { get => planeMinSize; set { planeMinSize = value; Refresh(); } }
        public float PlaneThreshold { get => planeThreshold; set { planeThreshold = value; Refresh(); } }
        public float MinDPVal { get => minDPVal; set { minDPVal = value; Refresh(); } }

        void Refresh()
        {
            Dopple.VideoFrame.RefreshConstant();
        }
    }
}
