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
            else
            {
                App.Recording = Dopple.Recording.Live;
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
        float planeMinSize = 0.25f;
        float planeThreshold = 0.035f;
        float minDPVal = 0.9f;
        float maxCoverage = 10.0f;
        int blur = 2;
        int depthLod = 3;
        int frameDelta = 1;
        float maxMatchDist = 0.2f;

        public event EventHandler<EventArgs> OnSettingsChanged;

        public float PlaneMinSize { get => planeMinSize; set { planeMinSize = value; Refresh(); } }
        public float PlaneThreshold { get => planeThreshold; set { planeThreshold = value; Refresh(); } }
        public float MinDPVal { get => minDPVal; set { minDPVal = value; Refresh(); } }
        public float MaxCoverage { get => maxCoverage; set { maxCoverage = value; Refresh(); } }
        public int Blur { get => blur; set { blur = value; Refresh(); } }
        public int DepthLod { get => depthLod; set { depthLod = value; Refresh(); } }
        public int FrameDelta { get => frameDelta; set { frameDelta = value; Refresh(); } }
        public float MaxMatchDist { get => maxMatchDist; set { maxMatchDist = value; Refresh(); } }

        void Refresh()
        {
            Dopple.VideoFrame.RefreshConstant();
            OnSettingsChanged?.Invoke(this, new EventArgs());
        }
    }
}
