using System;
using System.Windows;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using wf = System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Forms;

namespace Planes
{
    class PtsRenderer : IRenderer
    {
        public Dopple.Recording ActiveRecording => App.Recording;
        public Settings Settings => App.Settings;
        Matrix4 projectionMat = Matrix4.CreatePerspectiveFieldOfView(60 * (float)Math.PI / 180.0f, 1, 0.05f, 20.0f) *
            Matrix4.CreateScale(new Vector3(-1, 1, 1));
        Vector3 curPos = Vector3.Zero;
        Vector3 mouseDownPivot;
        Vector2? mouseDownPt;
        float xRot = 0.0f;
        float xRotDn;
        Vector3 worldPivot = new Vector3(0, 0, -5);
        Vector3 spivot = Vector3.Zero;

        VideoVis[] videoVis = new VideoVis[2];
        RenderTarget[] quads = new RenderTarget[2];
        DepthPtsVis[] depthVis = new DepthPtsVis[2];
        MatchVis matchVis;
        Selection selVis;
        RenderTarget pickTarget;

        float yRot = 0;
        float yRotDn;
        float pickedDepth = 0;

        Matrix4 rotMatrix = Matrix4.Identity;
        Matrix4 rotMatrixDn;
        Vector3 curPosDn;
        Vector3 wOffset;

        int currentWidth;
        int currentHeight;

        public PtsRenderer()
        {
        }



        public Matrix4 viewMat
        {
            get
            {
                Matrix4 lookTrans = this.rotMatrix * Matrix4.CreateTranslation(curPos);
                return lookTrans.Inverted();
            }
        }

        public float XRotDn { get => xRotDn; set => xRotDn = value; }

        public override void Load()
        {
            for (int i = 0; i < 2; ++i)
            {
                videoVis[i] = new VideoVis(i);
                depthVis[i] = new DepthPtsVis(i, true);
            }
            matchVis = new MatchVis();
            selVis = new Selection();
        }


        public override void Paint()
        {
            FrameBuffer.BindNone();
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Matrix4 viewProj = viewMat * projectionMat;
            quads[0].Use();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            for (int i = 0; i < 2; ++i)
            {
                depthVis[i].Render(viewProj, false);
            }
            this.selVis.Draw(viewProj);
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
            pickTarget = new RenderTarget(1024, 1024, new TextureR32());
            quads[0] = new RenderTarget(width / 2, height);
            quads[1] = new RenderTarget(width / 2, height);
            FrameBuffer.SetViewPortSize(width, height);
        }

        public override void MouseUp(int x, int y)
        {
            mouseDownPt = null;
        }

        public override void MouseDn(int x, int y, wf.MouseButtons button)
        {
            if (button == wf.MouseButtons.Middle && x < currentWidth / 2)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0 &&
                    (Keyboard.Modifiers & ModifierKeys.Control) == 0)
                {
                    int px = x * 2;
                    float depth = DoPick(true, px, y);
                    if (depth != 0)
                        pickedDepth = depth * 2 - 1;

                    float sx = ((float)px / (float)currentWidth * 2) - 1;
                    float sy = 1 - ((float)y / (float)currentHeight * 2);
                    Matrix4 vpinv = this.viewMat * this.projectionMat;
                    vpinv.Invert();
                    Vector3 wsPos =
                        Vector3.TransformPerspective(new Vector3(sx, sy, pickedDepth), vpinv);
                    selVis.wPos = wsPos;
                    this.worldPivot = wsPos;
                    this.mouseDownPivot = this.worldPivot;
                    this.spivot = Vector3.TransformPerspective(worldPivot, this.viewMat * this.projectionMat);
                }
                Matrix4 vproj = this.viewMat * this.projectionMat;
                vproj.Invert();
                Vector3 wpos0 = Vector3.TransformPerspective(this.spivot, vproj);
                Vector3 wpos1 = Vector3.TransformPerspective(new Vector3(0, 0, spivot.Z), vproj);
                this.wOffset = wpos1 - wpos0;
                this.curPosDn = this.curPos - wOffset;

                this.mouseDownPt = new Vector2(x, y);
                this.rotMatrixDn = this.rotMatrix;
                yRotDn = yRot;
                XRotDn = xRot;
            }
        }

        public override void MouseMove(int x, int y, MouseButtons button)
        {
            Vector2 curPt = new Vector2(x, y);
            if (mouseDownPt != null)
            {
                if (button == wf.MouseButtons.Middle)
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
                            m.Z * zd) + this.wOffset;
                    }
                    else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        float distFromPivot = (this.curPosDn - mouseDownPivot).Length;
                        if (distFromPivot == 0)
                        {
                            mouseDownPivot = worldPivot;
                            distFromPivot = (this.curPosDn - mouseDownPivot).Length;
                        }
                        Vector3 zDir = (this.curPosDn - mouseDownPivot).Normalized();

                        Vector3 yDirFrm = Vector3.TransformVector(Vector3.UnitY, this.rotMatrixDn);
                        Vector3 xDir = Vector3.Cross(zDir, yDirFrm);
                        Vector3 yDir = Vector3.Cross(xDir, zDir);
                        xDir = Vector3.Cross(zDir, yDir);
                        yDir = Vector3.Cross(xDir, zDir);
                        xDir.Normalize();
                        yDir.Normalize();
                        zDir.Normalize();
                        Matrix3 mt = new Matrix3(-xDir, yDir, zDir);
                        float mouseMovedDist = (float)(curPt.X - mouseDownPt.Value.X) + (float)(curPt.Y - mouseDownPt.Value.Y);
                        float multipler = 1.0f - mouseMovedDist / 1000.0f;

                        this.rotMatrix = new Matrix4(mt);
                        this.curPos = mouseDownPivot + distFromPivot * multipler * zDir;
                        this.curPos += wOffset;
                    }
                    else
                    {
                        xRot = (float)(curPt.X - mouseDownPt.Value.X) * -0.002f;
                        yRot = (float)(curPt.Y - mouseDownPt.Value.Y) * 0.002f;

                        float distFromPivot = (this.curPosDn - mouseDownPivot).Length;
                        if (distFromPivot == 0)
                        {
                            mouseDownPivot = worldPivot;
                            distFromPivot = (this.curPosDn - mouseDownPivot).Length;
                        }
                        Vector3 zDir = (this.curPosDn - mouseDownPivot).Normalized();

                        Vector3 yDirFrm = Vector3.TransformVector(Vector3.UnitY, this.rotMatrixDn);
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
                        this.curPos += wOffset;
                    }
                }
                Invalidate();
            }
        }

        struct GLPixelf
        {
            public float r;
            public float g;
            public float b;
            public float a;

            public bool HasValue =>
                r != 0 || g != 0 || b != 0 || a != 0;

            public override string ToString()
            {
                return $"{r},{g},{b},{a}";
            }
        }

        public override void MouseWheel(int x, int y, int delta)
        {
            double multiplier = Math.Pow(2, -delta / (120.0 * 4));
            mouseDownPivot = worldPivot;
            Vector3 distFromPivot = this.curPos - mouseDownPivot;
            distFromPivot *= (float)multiplier;
            curPos = mouseDownPivot + distFromPivot;
        }

        float[] pixels = null;

        float DoPick(bool fullObjectPicking, int sx, int sy)
        {
            pickTarget.Use();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Matrix4 viewProj = viewMat * projectionMat;
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            for (int i = 0; i < 2; ++i)
            {
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.DepthTest);
                depthVis[i].Render(viewProj, true);
            }
            GL.Finish();
            if (pixels == null || pixels.Length != (pickTarget.Width * pickTarget.Height))
                pixels = new float[pickTarget.Width * pickTarget.Height];
            GL.ReadPixels<float>(0, 0, pickTarget.Width, pickTarget.Height, PixelFormat.Red, PixelType.Float, pixels);
            FrameBuffer.BindNone();

            sx = sx * pickTarget.Width / currentWidth;
            sy = sy * pickTarget.Height / currentHeight;
            int dp = 4;
            int mindist = dp * dp * 2 + 1;
            float minval = 0;
            for (int x = -dp; x <= dp; ++x)
            {
                for (int y = -dp; y <= dp; ++y)
                {
                    int d = (x * x) + (y * y);
                    int ay = sy + y;
                    int ax = sx + x;
                    if ((d >= mindist) ||
                        (ay < 0 || ay >= pickTarget.Height) ||
                        (ax < 0 || ax >= pickTarget.Width))
                        continue;

                    float v = pixels[(pickTarget.Height - ay - 1) * pickTarget.Width + ax];
                    if (v != 0)
                    {
                        mindist = d;
                        minval = v;
                    }
                }
            }
            return minval;
        }


    }
}
