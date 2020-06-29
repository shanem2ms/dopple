using System;
using System.Windows;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using wf = System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Planes
{
    class DepthRenderer : IRenderer
    {
        public Settings Settings => App.Settings;
        System.Timers.Timer renderTimer = new System.Timers.Timer();
        Matrix4 projectionMat = Matrix4.CreatePerspectiveFieldOfView(60 * (float)Math.PI / 180.0f, 1, 0.05f, 20.0f) *
            Matrix4.CreateScale(new Vector3(-1, 1, 1));
        Vector3 viewOffset = Vector3.Zero;
        float viewScale = 1.0f;

        VideoVis[] videoVis = new VideoVis[2];
        DepthVis[] depthVis = new DepthVis[2];
        DeviceMotionVis dmv = null;
        RenderTarget[] quads = new RenderTarget[2];
        Matrix4 rotMatrix = Matrix4.Identity;

        int currentWidth;
        int currentHeight;

        public DepthRenderer()
        {
            renderTimer.Interval = 1.0f / 60.0f;
            renderTimer.Elapsed += RenderTimer_Elapsed;
        }

        private void RenderTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Invalidate();
        }

        public override void Load()
        {
            Dopple.VideoFrame.RefreshConstant();

            for (int i = 0; i < 2; ++i)
            {
                videoVis[i] = new VideoVis(i); 
                depthVis[i] = new DepthVis(i);
            }
            dmv = new DeviceMotionVis();
            renderTimer.Start();
        }

        Matrix4 ViewProj
        {
            get
            {
                Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 1, 0, 1, 1, 0);
                Matrix4 modelview = Matrix4.CreateScale(viewScale) * Matrix4.CreateTranslation(-0.5f, -0.5f, 0) * Matrix4.CreateRotationZ(-(float)Math.PI / 2.0f) *
                    Matrix4.CreateTranslation(0.5f, 0.5f, 0) * Matrix4.CreateTranslation(Vector3.Multiply(viewOffset, new Vector3(2, -1, 1)));
                return modelview * projection;
            }
        }

        public override void Paint()
        {
            FrameBuffer.BindNone();
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            Matrix4 viewProj = ViewProj;

            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            quads[0].Use();
            videoVis[0].Render(viewProj);
            depthVis[0].Render(viewProj);
            quads[1].Use();
            //videoVis[1].Render(viewProj);
            //depthVis[1].Render(viewProj);
            dmv.Render(viewProj);
            FrameBuffer.BindNone();
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            quads[0].Draw(new Vector4(-1, -1, 1, 2));
            quads[1].Draw(new Vector4(0, -1, 1, 2));
        }

        public override void Resize(int width, int height)
        {
            this.currentWidth = width;
            this.currentHeight = height;
            quads[0] = new RenderTarget(width / 2, height);
            quads[1] = new RenderTarget(width / 2, height);
            FrameBuffer.SetViewPortSize(width, height);
        }

        Vector3? mouseDnPt;
        Vector3 offsetDn;
        public override void MouseUp(int x, int y)
        {
            mouseDnPt = null;
        }

        public override void MouseDn(int x, int y, wf.MouseButtons button)
        {
            mouseDnPt = new Vector3(x, y, 0);
            offsetDn = viewOffset;
        }

        public override void MouseMove(int x, int y, MouseButtons button)
        {
            if (mouseDnPt != null)
            {
                Vector3 invWH = new Vector3(1.0f / currentWidth, 1.0f / currentHeight, 0);
                viewOffset = offsetDn + Vector3.Multiply((new Vector3(x, y, 0) - mouseDnPt.Value), invWH);
                Invalidate();
            }
        }

        public override void MouseWheel(int x, int y, int delta)
        {
            float oldScale = viewScale;
            double lVs = Math.Log10(viewScale) + (double)delta / 2400.0f;
            viewScale = (float)Math.Pow(10.0, lVs);
            viewOffset += new Vector3(0.25f, 0.5f, 0) * (oldScale - viewScale);
        }

        public override void Action(int param) { }
    }
}
