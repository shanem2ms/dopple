using System;
using System.Windows;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;

namespace Planes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dopple.Recording ActiveRecording => App.Recording;
        VideoVis videoVis;
        DepthVis depthVis;
        System.Timers.Timer renderTimer = new System.Timers.Timer();
        Matrix4 projectionMat = Matrix4.CreatePerspectiveFieldOfView(60 * (float) Math.PI / 180.0f, 1, 0.5f, 50.0f);
        Matrix4 viewMat = Matrix4.Identity;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            renderTimer.Interval = 1.0f / 60.0f;
            renderTimer.Elapsed += RenderTimer_Elapsed;
        }

        private void RenderTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.glControl.Invalidate();
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, glControl.ClientRectangle.Width, glControl.ClientRectangle.Height);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            glControl.Paint += GlControl_Paint;
            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseUp += GlControl_MouseUp;
            videoVis = new VideoVis();
            depthVis = new DepthVis();
            renderTimer.Start();
        }

        private void GlControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
        }

        private void GlControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
        }

        private void GlControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
        }

        private void GlControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Matrix4 viewProj = viewMat * projectionMat;
            videoVis.Render();
            depthVis.Render(viewProj);
            glControl.SwapBuffers();
        }
    }
}
