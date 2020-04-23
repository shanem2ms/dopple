using System;
using System.Windows;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using wf = System.Windows.Forms;
using System.Windows.Input;

namespace Planes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dopple.Recording ActiveRecording => App.Recording;
        public Settings Settings => App.Settings;
        System.Timers.Timer renderTimer = new System.Timers.Timer();
        Matrix4 projectionMat = Matrix4.CreatePerspectiveFieldOfView(60 * (float)Math.PI / 180.0f, 1, 0.5f, 50.0f) *
            Matrix4.CreateScale(new Vector3(-1, 1, 1));
        Matrix4 viewMat = Matrix4.Identity;
        Vector3 curPos = Vector3.Zero;
        Vector3 mouseDownPivot;
        System.Drawing.Point? mouseDownPt;
        float xRot = 0.0f;
        float xRotDn;
        Vector3 worldPivot = new Vector3(0, 0, -5);

        VideoVis[] videoVis = new VideoVis[2];
        RenderTarget []vidRT = new RenderTarget[2];
        DepthPtsVis []depthVis = new DepthPtsVis[2];
        RenderTarget []ptsRT = new RenderTarget[2];
        MatchesVis matchesVis;

        float yRot = 0;
        float yRotDn;

        Matrix4 rotMatrix = Matrix4.Identity;
        Matrix4 rotMatrixDn;
        Vector3 curPosDn;


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
            FrameBuffer.SetViewPortSize(glControl.ClientRectangle.Width, glControl.ClientRectangle.Height);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            glControl.Paint += GlControl_Paint;
            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseUp += GlControl_MouseUp;
            Registry.LoadAllPrograms();

            for (int i = 0; i < 2; ++i)
            {
                videoVis[i] = new VideoVis(i);
                depthVis[i] = new DepthPtsVis(0, i == 1);
                vidRT[i] = new RenderTarget(1024, 1024);
                ptsRT[i] = new RenderTarget(1024, 1024);
            }
            matchesVis = new MatchesVis();
            renderTimer.Start();
        }

        private void GlControl_Paint(object sender, wf.PaintEventArgs e)
        {
            Matrix4 lookTrans = this.rotMatrix * Matrix4.CreateTranslation(curPos);
            this.viewMat = lookTrans.Inverted();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Matrix4 viewProj = viewMat * projectionMat;
            for (int i = 0; i < 2; ++i)
            {
                ptsRT[i].Use();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.DepthTest);
                depthVis[i].Render(viewProj);
            }
            FrameBuffer.BindNone();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            ptsRT[0].Draw(new Vector4(-1, -1, 1, 2));
            ptsRT[1].Draw(new Vector4(0, -1, 1, 2));

            glControl.SwapBuffers();
        }

        void QuadRender(Matrix4 viewProj)
        {
            for (int i = 0; i < 2; ++i)
            {
                vidRT[i].Use();
                videoVis[i].Render();

                ptsRT[i].Use();
                depthVis[i].Render(viewProj);
            }
            FrameBuffer.BindNone();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            GL.Disable(EnableCap.DepthTest);
            vidRT[0].Draw(new Vector4(-1, 0, 1, 1));
            ptsRT[0].Draw(new Vector4(0, 0, 1, 1));
            vidRT[1].Draw(new Vector4(-1, -1, 1, 1));
            ptsRT[1].Draw(new Vector4(0, -1, 1, 1));
        }


        private void GlControl_MouseUp(object sender, wf.MouseEventArgs e)
        {
            mouseDownPt = null;
        }

        private void GlControl_MouseWheel(object sender, wf.MouseEventArgs e)
        {
            double multiplier = Math.Pow(2, -e.Delta / (120.0 * 4));
            mouseDownPivot = worldPivot;
            Vector3 distFromPivot = this.curPos - mouseDownPivot;
            distFromPivot *= (float)multiplier;
            curPos = mouseDownPivot + distFromPivot;
        }

        Vector2 ScreenToViewport(System.Drawing.Point pt)
        {
            return new Vector2(((float)pt.X / (float)glControl.Width) * 2 - 1.0f,
                             1.0f - ((float)pt.Y / (float)glControl.Height) * 2);
        }

        private void GlControl_MouseMove(object sender, wf.MouseEventArgs e)
        {
            System.Drawing.Point curPt = e.Location;
            if (mouseDownPt != null)
            {
                if (e.Button == wf.MouseButtons.Middle)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        float xOffset = (float)(curPt.X - mouseDownPt.Value.X) * 0.001f;
                        float yOffset = (float)(curPt.Y - mouseDownPt.Value.Y) * 0.001f;

                        Matrix4 viewInv = this.viewMat.Inverted();
                        Vector3 zd = Vector3.TransformNormal(Vector3.UnitZ, viewInv).Normalized();
                        Vector3 xd = Vector3.TransformNormal(Vector3.UnitX, viewInv).Normalized();
                        Vector3 yd = Vector3.TransformNormal(Vector3.UnitY, viewInv).Normalized();

                        Vector3 m = new Vector3(xOffset, yOffset, 0);
                        curPos = curPosDn + (m.X * xd + m.Y * yd +
                            m.Z * zd);
                    }
                    else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        float xOffset = (float)(curPt.X - mouseDownPt.Value.X) * 0.001f;
                        float yOffset = (float)(curPt.Y - mouseDownPt.Value.Y) * 0.001f;

                        Matrix4 viewInv = this.viewMat.Inverted();
                        Vector3 zd = Vector3.TransformNormal(Vector3.UnitZ, viewInv).Normalized();
                        Vector3 xd = Vector3.TransformNormal(Vector3.UnitX, viewInv).Normalized();
                        Vector3 yd = Vector3.TransformNormal(Vector3.UnitY, viewInv).Normalized();

                        Vector3 m = new Vector3(0, 0, yOffset);
                        curPos = curPosDn + (m.X * xd + m.Y * yd +
                            m.Z * zd);
                    }
                    else
                    {
                        xRot = (float)(curPt.X - mouseDownPt.Value.X) * -0.002f;
                        yRot = (float)(curPt.Y - mouseDownPt.Value.Y) * 0.002f;

                        float distFromPivot = (curPosDn - mouseDownPivot).Length;
                        if (distFromPivot == 0)
                        {
                            mouseDownPivot = worldPivot;
                            distFromPivot = (curPosDn - mouseDownPivot).Length;
                        }

                        Vector3 zDir = (curPosDn - mouseDownPivot).Normalized();
                        Vector3 yDirFrm = Vector3.TransformVector(Vector3.UnitY,
                            this.rotMatrixDn);
                        Vector3 xDir = Vector3.Cross(zDir, yDirFrm);
                        Vector3 yDir = Vector3.Cross(xDir, zDir);
                        zDir = Quaternion.FromAxisAngle(yDir, xRot) *
                            Quaternion.FromAxisAngle(xDir, yRot) * zDir;
                        xDir = Vector3.Cross(zDir, yDir);
                        yDir = Vector3.Cross(xDir, zDir);
                        xDir.Normalize();
                        yDir.Normalize();
                        zDir.Normalize();
                        Matrix3 mt = new Matrix3(-xDir, yDir, zDir);

                        this.rotMatrix = new Matrix4(mt);
                        this.curPos = mouseDownPivot + distFromPivot * zDir;
                    }
                }
                glControl.Invalidate();
            }
        }

        private void GlControl_MouseDown(object sender, wf.MouseEventArgs e)
        {
            if (e.Button == wf.MouseButtons.Middle)
            {
                mouseDownPivot = worldPivot;
                mouseDownPt = e.Location;
                this.rotMatrixDn = this.rotMatrix;
                this.curPosDn = this.curPos;
                yRotDn = yRot;
                xRotDn = xRot;
            }
        }

        private void Back_Clicked(object sender, RoutedEventArgs e)
        {
            this.ActiveRecording.CurrentFrameIdx--;
        }
        private void Next_Clicked(object sender, RoutedEventArgs e)
        {
            this.ActiveRecording.CurrentFrameIdx++;
        }

    }
}
