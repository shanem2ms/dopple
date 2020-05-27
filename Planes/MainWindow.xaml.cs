using System;
using System.Windows;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using wf = System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Documents;

namespace Planes
{

    public abstract class IRenderer
    {
        public delegate void InvalidateDel();
        public InvalidateDel Invalidate;

        public abstract void Load();
        public abstract void Paint();
        public abstract void Resize(int width, int height);
        public abstract void MouseUp(int x, int y);
        public abstract void MouseDn(int x, int y, wf.MouseButtons button);
        public abstract void MouseMove(int x, int y, wf.MouseButtons button);
        public abstract void MouseWheel(int x, int y, int delta);
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public Dopple.Recording ActiveRecording => App.Recording;
        IRenderer[] renderers = new IRenderer[3] {
                new VideoRenderer(),
                new DepthRenderer(),
                new PtsRenderer() };
        System.Timers.Timer renderTimer = new System.Timers.Timer();

        public Settings Settings => App.Settings;

        IRenderer ar;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            foreach (var r in renderers)
                r.Invalidate = OnInvalidate;
            ar = renderers[2];
        }

        void OnInvalidate()
        {
            glControl.Invalidate();
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            foreach (var r in renderers)
                r.Resize(glControl.ClientRectangle.Width, glControl.ClientRectangle.Height);
        }

        private void RenderTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            glControl.Invalidate();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            glControl.Paint += GlControl_Paint;
            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseUp += GlControl_MouseUp;
            glControl.MouseWheel += GlControl_MouseWheel;
            renderTimer.Interval = 1.0f / 60.0f;
            renderTimer.Elapsed += RenderTimer_Elapsed;

            Registry.LoadAllPrograms();
            foreach (var r in renderers)
                r.Load();

            renderTimer.Start();
        }

        private void GlControl_Paint(object sender, wf.PaintEventArgs e)
        {            
            ar.Paint();
            glControl.SwapBuffers();
        }


        private void GlControl_MouseUp(object sender, wf.MouseEventArgs e)
        {
            ar.MouseUp(e.X, e.Y);
        }

        private void GlControl_MouseWheel(object sender, wf.MouseEventArgs e)
        {
            ar.MouseWheel(e.X, e.Y, e.Delta);
        }

        Vector2 ScreenToViewport(System.Drawing.Point pt)
        {
            return new Vector2(((float)pt.X / (float)glControl.Width) * 2 - 1.0f,
                             1.0f - ((float)pt.Y / (float)glControl.Height) * 2);
        }

        private void GlControl_MouseMove(object sender, wf.MouseEventArgs e)
        {
            ar.MouseMove(e.X, e.Y, e.Button);
        }

        private void GlControl_MouseDown(object sender, wf.MouseEventArgs e)
        {
            ar.MouseDn(e.X, e.Y, e.Button);
        }

        private void Back_Clicked(object sender, RoutedEventArgs e)
        {
            App.Recording.CurrentFrameIdx--;
        }
        private void Next_Clicked(object sender, RoutedEventArgs e)
        {
            App.Recording.CurrentFrameIdx++;
        }

    }
}
