using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using wf = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Forms;
using System.Linq;
using Dopple;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Documents;
using OpenCvSharp;
using System.Diagnostics;

namespace Planes
{
    class SceneRenderer : IRenderer, INotifyPropertyChanged
    {
        public Dopple.Recording ActiveRecording => App.Recording;
        public Settings Settings => App.Settings;
        Matrix4 projectionMat = Matrix4.CreatePerspectiveFieldOfView(60 * (float)Math.PI / 180.0f, 1, 0.05f, 10.0f) *
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
        SceneVis sceneVis;
        WorldVis worldVis;
        CameraTrackVis camTrackVis;
        Selection selVis;
        RenderTarget pickTarget;

        float yRot = 0;
        float yRotDn;

        Matrix4 rotMatrix = Matrix4.Identity;
        Matrix4 rotMatrixDn;
        Vector3 curPosDn;
        Vector3 wOffset;

        int currentWidth;
        int currentHeight;
        bool isDirty = true;


        int[] matches;
        Vector3 offsetTranslation = Vector3.Zero;
        Vector3 offsetTranslationMsDn = Vector3.Zero;
        Vector3[] worldPts = null;
        float multiplier = 0.01f;
        public float OffsetTranslationX
        {
            get => offsetTranslation.X / multiplier;
            set { offsetTranslation.X = value * multiplier; isDirty = true; }
        }
        public float OffsetTranslationY
        {
            get => offsetTranslation.Y / multiplier;
            set { offsetTranslation.Y = value * multiplier; isDirty = true; }
        }
        public float OffsetTranslationZ
        {
            get => offsetTranslation.Z / multiplier;
            set { offsetTranslation.Z = value * multiplier; isDirty = true; }
        }

        Vector3 offsetRotation = Vector3.Zero;
        Vector3 offsetRotationMsDn = Vector3.Zero;
        float rmultiplier = 0.001f;
        public float OffsetRotationX
        {
            get => offsetRotation.X / rmultiplier;
            set
            {
                offsetRotation.X = value * rmultiplier; isDirty = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetRotationX"));
            }
        }
        public float OffsetRotationY
        {
            get => offsetRotation.Y / rmultiplier;
            set
            {
                offsetRotation.Y = value * rmultiplier; isDirty = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetRotationY"));
            }
        }
        public float OffsetRotationZ
        {
            get => offsetRotation.Z / rmultiplier;
            set
            {
                offsetRotation.Z = value * rmultiplier; isDirty = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetRotationZ"));
            }
        }


        public float TotalDist { get; private set; } = 0;

        public SceneRenderer()
        {
            App.Recording.OnFrameChanged += Recording_OnFrameChanged;
        }

        private void Recording_OnFrameChanged(object sender, int e)
        {
            isDirty = true;
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
            sceneVis = new SceneVis();
            worldVis = new WorldVis();
            camTrackVis = new CameraTrackVis();
            for (int i = 0; i < 2; ++i)
            {
                videoVis[i] = new VideoVis(i);
            }
            selVis = new Selection();
        }

        static Vector3 RotX(float a, Vector3 v)
        {
            float cosA = (float)Math.Cos(a);
            float sinA = (float)Math.Sin(a);
            return new Vector3(v.X, v.Y * cosA - v.Z * sinA, v.Y * sinA + v.Z * cosA);
        }

        static Vector3 RotY(float b, Vector3 v)
        {
            float cosB = (float)Math.Cos(b);
            float sinB = (float)Math.Sin(b);
            return new Vector3(v.X * cosB - v.Z * sinB, v.Y, v.X * sinB + v.Z * cosB);
        }

        static Vector3 RotZ(float c, Vector3 v)
        {
            float cosC = (float)Math.Cos(c);
            float sinC = (float)Math.Sin(c);
            return new Vector3(v.X * cosC - v.Y * sinC, v.X * sinC + v.Y * cosC, v.Z);
        }

        NrmPt[] GetNrmPts(int curFrame)
        {
            VideoFrame vf = App.Recording.Frames[curFrame].vf;
            int dw = vf.DepthWidth;
            int dh = vf.DepthHeight;

            float invWidth = 1.0f / vf.ImageWidth * dw;
            float invHeight = 1.0f / vf.ImageHeight * dh;

            var pts = vf.CalcDepthPoints();
            NrmPt[] ptArray = new NrmPt[dh * dw];
            foreach (var p in pts)
            {
                ptArray[p.Key] = new NrmPt() { pt = p.Value.pt, spt = p.Value.spt };
            }

            for (int y = 0; y < (dh - 1); ++y)
            {
                for (int x = 0; x < (dw - 1); ++x)
                {
                    if (ptArray[(y) * dw + (x)] == null)
                        continue;
                    NrmPt[] borders = { ptArray[(y + 1) * dw + (x + 1)],
                        ptArray[(y + 1) * dw + (x)],
                        ptArray[(y) * dw + (x + 1)],
                        ptArray[(y) * dw + (x)] };

                    var validPts = borders.Where(a => a != null);
                    if (validPts.Count() < 3)
                        continue;
                    NrmPt[] vpts = validPts.ToArray();
                    ptArray[(y) * dw + (x)].nrm = Vector3.Cross((vpts[0].pt - vpts[1].pt).Normalized(),
                                            (vpts[2].pt - vpts[0].pt).Normalized());
                }
            }

            return ptArray;
        }

        Matrix4 []frameMatrix = null;
        int nFramesProcessed = 0;
        int nFramesShown = 0;
        bool framesReady = false;

        NrmPt[][] framePts = null;

        Vector4 FromD(Vector4d v)
        {
            return new Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
        }
        Vector4d ToD(Vector4 v)
        {
            return new Vector4d(v.X, v.Y, v.Z, v.W);
        }


        Matrix4 FromD(Matrix4d m)
        {
            return new Matrix4(FromD(m.Row0), FromD(m.Row1), FromD(m.Row2), FromD(m.Row3));
        }
        Matrix4d ToD(Matrix4 m)
        {
            return new Matrix4d(ToD(m.Row0), ToD(m.Row1), ToD(m.Row2), ToD(m.Row3));
        }

        void StartBkProcess()
        {
            frameMatrix = new Matrix4[App.Recording.NumFrames];
            Thread t = new Thread(() =>
            {
                int startFrame = 0;
                int endFrame = App.Recording.NumFrames - 1;
                Matrix4d totalMatrix = Matrix4d.Identity;

                frameMatrix[0] = FromD(totalMatrix);
                var vfcam = App.Recording.Frames[0].vf;
                float[] camvals = new float[6]
                {
                    vfcam.cameraCalibrationVals.X,
                    vfcam.cameraCalibrationVals.Y,
                    vfcam.cameraCalibrationVals.Z,
                    vfcam.cameraCalibrationVals.W,
                    vfcam.cameraCalibrationDims.X,
                    vfcam.cameraCalibrationDims.Y
                };

                int dw = App.Recording.Frames[0].vf.DepthWidth;
                int dh = App.Recording.Frames[0].vf.depthHeight;
                for (int curFrame = startFrame; curFrame < endFrame; ++curFrame)
                {
                    Matrix4 outMat;
                    var vf0 = App.Recording.Frames[curFrame].vf;
                    var vf1 = App.Recording.Frames[curFrame + 1].vf;
                    Aligner.AlignBest(vf0.depthData, vf1.depthData, camvals,
                        vf0.depthWidth, vf0.depthHeight, out outMat);

                    totalMatrix = ToD(outMat) * totalMatrix;

                    Aligner.AddWorldPoints(vf1.depthData, camvals, vf1.depthWidth, vf1.depthHeight, 
                        FromD(totalMatrix));
                    frameMatrix[curFrame + 1] = FromD(totalMatrix);
                    nFramesProcessed = curFrame + 2;
                    isDirty = true;
                    if ((curFrame % 10) == 0)
                    {
                        worldPts = Aligner.GetWorldPoints();
                    }
                }

                framesReady = true;
            });
            t.Start();
        }

        void UpdateFrame()
        {
            if (framePts == null)
                framePts = new NrmPt[App.Recording.NumFrames][];
            for (int idx = 0; idx <= App.Recording.CurrentFrameIdx; ++idx)
            {
                if (framePts[idx] == null)
                    framePts[idx] = GetNrmPts(idx);
            }
            var vfcam = App.Recording.Frames[0].vf;

            var framePtsList = frameMatrix.Zip(framePts, (x, y) => new Tuple<Matrix4, NrmPt[]>(x, y)).ToList().GetRange(0,
                App.Recording.CurrentFrameIdx + 1);
            this.sceneVis.UpdateFrame(vfcam, framePtsList, App.Recording.CurrentFrameIdx, 10);
            if (this.worldPts != null)
            {
                this.worldVis.UpdateFrame(this.worldPts);
            }
            nFramesShown = nFramesProcessed;
            this.camTrackVis.UpdateFrame(vfcam, frameMatrix, App.Recording.CurrentFrameIdx, nFramesShown);
            isDirty = false;
        }

        void RecalcTotalDist(Vector3[] parr0, Vector3[] pnrm0, Vector3[] parr1, int[] matches)
        {
            float totalDist = 0;
            for (int idx = 0; idx < matches.Length; idx += 2)
            {
                float dp = Vector3.Dot((parr0[matches[idx]] - parr1[matches[idx + 1]]), pnrm0[matches[idx]]);
                totalDist += dp * dp;

            }

            TotalDist = totalDist;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalDist"));
        }


        public override void Paint()
        {
            if (frameMatrix == null)
            {
                StartBkProcess();
            }
            if (isDirty)
            {
                UpdateFrame();
            }
            else if (nFramesShown != nFramesProcessed)
            {

            }
            FrameBuffer.BindNone();
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            Matrix4 viewProj = viewMat * projectionMat;
            for (int i = 0; i < 2; ++i)
            {
                quads[i].Use();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.DepthTest);
                if (i == 0)
                    worldVis.Render(viewProj, false, false);
                else
                    camTrackVis.Render(viewProj, false, false);
                this.selVis.Draw(viewProj);
            }
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
            pickTarget = new RenderTarget(1024, 1024, new TextureRgba128());
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
            if (button == wf.MouseButtons.Middle)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0 &&
                    (Keyboard.Modifiers & ModifierKeys.Control) == 0)
                {
                    int px = x * 2;
                    if (px >= currentWidth)
                        px -= currentWidth;
                    GLPixelf wsPos = DoPick(true, px, y);

                    float sx = ((float)px / (float)currentWidth * 2) - 1;
                    float sy = 1 - ((float)y / (float)currentHeight * 2);
                    Matrix4 vpinv = this.viewMat * this.projectionMat;
                    vpinv.Invert();
                    selVis.wPos = new Vector3(wsPos.r, wsPos.g, wsPos.b);
                    this.worldPivot = selVis.wPos;
                    this.mouseDownPivot = this.worldPivot;
                    this.spivot = Vector3.TransformPerspective(worldPivot, this.viewMat * this.projectionMat);
                }
                Matrix4 vproj = this.viewMat * this.projectionMat;
                vproj.Invert();
                Vector3 wpos0 = Vector3.TransformPerspective(this.spivot, vproj);
                Vector3 wpos1 = Vector3.TransformPerspective(new Vector3(0, 0, spivot.Z), vproj);
                this.wOffset = wpos1 - wpos0;
                this.curPosDn = this.curPos - wOffset;

                this.rotMatrixDn = this.rotMatrix;
                yRotDn = yRot;
                XRotDn = xRot;
            }
            else if (button == wf.MouseButtons.Left)
            {
                this.offsetTranslationMsDn = this.offsetTranslation;
            }
            this.mouseDownPt = new Vector2(x, y);
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
                else if (button == wf.MouseButtons.Left)
                {
                    float xOffset = (float)(curPt.X - mouseDownPt.Value.X) * 0.001f;
                    float yOffset = (float)(curPt.Y - mouseDownPt.Value.Y) * 0.001f;

                    Matrix4 viewInv = this.viewMat.Inverted();
                    Vector3 zd = Vector3.TransformNormal(Vector3.UnitZ, viewInv).Normalized();
                    Vector3 xd = Vector3.TransformNormal(Vector3.UnitX, viewInv).Normalized();
                    Vector3 yd = Vector3.TransformNormal(Vector3.UnitY, viewInv).Normalized();

                    Vector3 m = new Vector3(xOffset, yOffset, 0);
                    offsetTranslation = offsetTranslationMsDn + m;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetTranslationX"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetTranslationY"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetTranslationZ"));
                    isDirty = true;
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

        GLPixelf[] pixels = null;

        public event PropertyChangedEventHandler PropertyChanged;

        GLPixelf DoPick(bool fullObjectPicking, int sx, int sy)
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
                sceneVis.Render(viewProj, false, true);
            }
            GL.Finish();
            if (pixels == null || pixels.Length != (pickTarget.Width * pickTarget.Height))
                pixels = new GLPixelf[pickTarget.Width * pickTarget.Height];
            GL.ReadPixels<GLPixelf>(0, 0, pickTarget.Width, pickTarget.Height, PixelFormat.Rgba, PixelType.Float, pixels);
            FrameBuffer.BindNone();

            sx = sx * pickTarget.Width / currentWidth;
            sy = sy * pickTarget.Height / currentHeight;
            int dp = 4;
            int mindist = dp * dp * 2 + 1;
            GLPixelf minval = new GLPixelf();
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

                    GLPixelf v = pixels[(pickTarget.Height - ay - 1) * pickTarget.Width + ax];
                    if (v.HasValue)
                    {
                        mindist = d;
                        minval = v;
                    }
                }
            }
            return minval;
        }

        public override void Action(int param)
        {
        }
    }
}
