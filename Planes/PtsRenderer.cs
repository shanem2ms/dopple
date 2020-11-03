﻿using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using wf = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Forms;
using System.Linq;
using Dopple;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Planes
{
    class PtsRenderer : IRenderer, INotifyPropertyChanged
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
        DepthPtsVis[] depthVis = new DepthPtsVis[2];
        AttitudeVis attVis;
        MatchVis matchVis;
        Selection selVis;
        RenderTarget pickTarget;
        GridVis gridVis;

        float yRot = 0;
        float yRotDn;

        Matrix4 rotMatrix = Matrix4.Identity;
        Matrix4 rotMatrixDn;
        Vector3 curPosDn;
        Vector3 wOffset;

        Matrix4 alignMat = Matrix4.Identity;

        int currentWidth;
        int currentHeight;
        bool isDirty = true;


        int[] matches;
        Vector3 offsetTranslation = Vector3.Zero;
        Vector3 offsetTranslationMsDn = Vector3.Zero;
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

        Vector3 rotate_vector_by_quaternion(Vector3 v, Quaternion q)
        {
            // Extract the vector part of the quaternion
            Vector3 u = new Vector3(q.X, q.Y, q.Z);

            // Extract the scalar part of the quaternion
            float s = q.W;

            // Do the math
            return 2.0f * Vector3.Dot(u, v) * u
              + (s * s - Vector3.Dot(u, u)) * v
              + 2.0f * s * Vector3.Cross(u, v);
        }



        public PtsRenderer()
        {
            App.Recording.OnFrameChanged += Recording_OnFrameChanged;
            App.Settings.OnSettingsChanged += Settings_OnSettingsChanged;

            Vector3 v = new Vector3(10, 11, 12);
            Quaternion q = Quaternion.FromAxisAngle(new Vector3(1, 2, 3).Normalized(), 1.0f);
            Vector3 vp = q * v;
            Vector3 vp2 = rotate_vector_by_quaternion(v, q);
        }

        private void Settings_OnSettingsChanged(object sender, EventArgs e)
        {
            isDirty = true;
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
            for (int i = 0; i < 2; ++i)
            {
                videoVis[i] = new VideoVis(i);
                depthVis[i] = new DepthPtsVis(i);
            }
            matchVis = new MatchVis();
            selVis = new Selection();
            attVis = new AttitudeVis();
            gridVis = new GridVis();
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

        Matrix4 CalcGravRot(int curFrame)
        {
            MotionPoint[] mpts = App.Recording.Frames[curFrame].motionPoints;
            var mp = mpts[0];
            Vector3 grav = new Vector3((float)mp.gX, (float)mp.gY, (float)mp.gZ);
            grav.Normalize();
            Vector3 basis = -Vector3.UnitY;
            Quaternion qrot = new Quaternion(Vector3.Cross(basis, grav),
                (float)Math.Sqrt(grav.LengthSquared * basis.LengthSquared) + Vector3.Dot(grav, basis));
            return  Matrix4.CreateFromQuaternion(qrot);
        }

        NrmPt[][] GetNrmPts(int curFrame)
        {
            NrmPt[][] ptArrays = new NrmPt[2][];
            if (App.Recording.Frames.Count == 0)
                return null;
            for (int idx = 0; idx < 2; ++idx)
            {
                VideoFrame vf = App.Recording.Frames[curFrame + (idx * App.Settings.FrameDelta)].vf;
                Matrix4 gravRot = CalcGravRot(curFrame + (idx * App.Settings.FrameDelta));
                int dw = vf.DepthWidth;
                int dh = vf.DepthHeight;

                Matrix4 mat = idx == 0 ? gravRot : gravRot * this.alignMat;

                float invWidth = 1.0f / vf.ImageWidth * dw;
                float invHeight = 1.0f / vf.ImageHeight * dh;

                var pts = vf.CalcDepthPoints();
                NrmPt[] ptArray = new NrmPt[dh * dw];
                foreach (var p in pts)
                {
                    ptArray[p.Key] = new NrmPt() { pt = Vector3.TransformPosition(p.Value.pt, mat), spt = p.Value.spt };
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
                                                (vpts[2].pt - vpts[0].pt).Normalized()).Normalized();
                    }
                }

                ptArrays[idx] = ptArray.Where(p => p != null).ToArray();

            }

            return ptArrays;
        }

        void UpdateFrame()
        {
            if (App.Recording.Frames.Count == 0)
                return;
            int curFrame = App.Recording.CurrentFrameIdx;
            if (curFrame >= App.Recording.Frames.Count - App.Settings.FrameDelta)
                curFrame = App.Recording.Frames.Count - (App.Settings.FrameDelta + 1);

            Matrix4 mat = Matrix4.CreateTranslation(offsetTranslation) *
                Matrix4.CreateFromQuaternion(
                    Quaternion.FromEulerAngles(new Vector3(offsetRotation.X, offsetRotation.Y, offsetRotation.Z)));

            NrmPt[][] ptArrays = GetNrmPts(curFrame);
            for (int idx = 0; idx < 2; ++idx)
            {
                VideoFrame vf = App.Recording.Frames[curFrame + (idx * App.Settings.FrameDelta)].vf;
                var arr = ptArrays[idx];
                if (idx == 1)
                {
                    arr = ptArrays[idx].Select(n => new NrmPt()
                    {
                        pt = Vector3.TransformPosition(n.pt, mat),
                        nrm = n.nrm,
                        spt = n.spt
                    }).ToArray();
                }
                depthVis[idx].UpdateFrame(vf, arr, idx == 0 ? Vector3.Zero : Vector3.UnitZ);
                
                    }
            if (ptArrays[0].Length < 40000)
            {
                Vector3[] parr0 = ptArrays[0].Select(p => p.pt).ToArray();
                Vector3[] parr1 = ptArrays[1].Select(p => p.pt).ToArray();
                Vector3[] pnrm1 = ptArrays[0].Select(p => p.nrm).ToArray();
                matches = Aligner.FindMatches(parr0, parr1);

                for (int idx = 0; idx < parr1.Length; ++idx)
                {
                    parr1[idx] = Vector3.TransformPosition(parr1[idx], mat);
                }
                RecalcTotalDist(parr0, pnrm1, parr1, matches);

                ptArrays[1] = ptArrays[1].Select(n => new NrmPt()
                {
                    pt = Vector3.TransformPosition(n.pt, mat),
                    nrm = n.nrm,
                    spt = n.spt
                }).ToArray();

                matchVis.UpdateFrame(App.Recording.CurrentFrame.vf, ptArrays, matches);
            }
            else
            {
                matches = new int[0];
                matchVis.UpdateFrame(App.Recording.CurrentFrame.vf, ptArrays, matches);
            }
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


        struct EulerAngles
        {
            public double roll, pitch, yaw;
        };

        static EulerAngles ToEulerAngles(Quaternion q)
        {
            EulerAngles angles;

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.pitch = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.yaw = Math.PI / 2 * Math.Sign(sinp); // use 90 degrees if out of range
            else
                angles.yaw = Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.roll = Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }


        void Align(out Vector3 outOffset, out Vector3 outERot)
        {
            int curFrame = App.Recording.CurrentFrameIdx;
            if (curFrame >= App.Recording.Frames.Count - App.Settings.FrameDelta)
                curFrame = App.Recording.Frames.Count - (App.Settings.FrameDelta + 1);
            var vf0 = App.Recording.Frames[curFrame].vf;
            var vf1 = App.Recording.Frames[curFrame + App.Settings.FrameDelta].vf;
            float[] camvals = new float[6]
            {
                vf0.cameraCalibrationVals.X,
                vf0.cameraCalibrationVals.Y,
                vf0.cameraCalibrationVals.Z,
                vf0.cameraCalibrationVals.W,
                vf0.cameraCalibrationDims.X,
                vf0.cameraCalibrationDims.Y
            };
            Matrix4 outMat;
            Aligner.AlignBest(vf0.depthData, vf1.depthData, camvals,
                vf0.depthWidth, vf0.depthHeight, App.Settings.MaxMatchDist, out outMat);
            Quaternion q = outMat.ExtractRotation();
            EulerAngles ea = ToEulerAngles(q);
            outERot = new Vector3((float)ea.pitch, (float)ea.yaw, (float)ea.roll);
            outOffset = outMat.ExtractTranslation();
        }

        void AlignLod(out Vector3 outOffset, out Vector3 outERot)
        {
            int curFrame = App.Recording.CurrentFrameIdx;
            if (curFrame >= App.Recording.Frames.Count - App.Settings.FrameDelta)
                curFrame = App.Recording.Frames.Count - (App.Settings.FrameDelta + 1);
            NrmPt[][] ptArrays = GetNrmPts(curFrame);
            Vector3[] parr0 = ptArrays[0].Select(p => p.pt).ToArray();
            Vector3[] nrm0 = ptArrays[0].Select(p => p.nrm).ToArray();
            Vector3[] parr1 = ptArrays[1].Select(p => p.pt).ToArray();
            var vf = App.Recording.CurrentFrame.vf;
            Matrix4 outMat;
            Aligner.Align(parr0, nrm0, parr1, vf.DepthWidth, vf.DepthHeight, App.Settings.MaxMatchDist, out outMat);
            Quaternion q = outMat.ExtractRotation();
            EulerAngles ea = ToEulerAngles(q);
            outERot = new Vector3((float)ea.pitch, (float)ea.yaw, (float)ea.roll);
            outOffset = outMat.ExtractTranslation();
        }

        public override void Paint()
        {
            if (isDirty)
            {
                UpdateFrame();
                isDirty = false;
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
                gridVis.Render(viewProj);
                if (i == 0)
                {
                    depthVis[0].Render(viewProj, false, false);
                    attVis.Render(viewProj);
                }
                else
                {
                    matchVis.Render(viewProj, false);
                    depthVis[0].Render(viewProj, true, false);
                    depthVis[1].Render(viewProj, true, false);
                }
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
                depthVis[i].Render(viewProj, false, true);
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
            if (param == 0)
            {
                Vector3 offset;
                Vector3 eRot;
                Align(out offset, out eRot);
                offsetTranslation = offset;
                offsetRotation = eRot;

                Matrix4 amat1 = Matrix4.CreateTranslation(offsetTranslation) *
                    Matrix4.CreateFromQuaternion(
                    Quaternion.FromEulerAngles(new Vector3(offsetRotation.X, offsetRotation.Y, offsetRotation.Z)));

            }
            else if (param == 1)
            {
                offsetTranslation = Vector3.Zero;
                offsetRotation = Vector3.Zero;
                this.alignMat = Matrix4.Identity;
            }
            else if (param == 2)
            {
                Matrix4 amat = Matrix4.CreateTranslation(offsetTranslation) *
                    Matrix4.CreateFromQuaternion(
                    Quaternion.FromEulerAngles(new Vector3(offsetRotation.X, offsetRotation.Y, offsetRotation.Z)));
                this.alignMat = amat;
                offsetTranslation = Vector3.Zero;
                offsetRotation = Vector3.Zero;
            }
            else if (param == 3)
            {
                Vector3 offset;
                Vector3 eRot;
                AlignLod(out offset, out eRot);
                offsetTranslation = offset;
                offsetRotation = eRot;

                Matrix4 amat1 = Matrix4.CreateTranslation(offsetTranslation) *
                    Matrix4.CreateFromQuaternion(
                    Quaternion.FromEulerAngles(new Vector3(offsetRotation.X, offsetRotation.Y, offsetRotation.Z)));
            }
            else if (param == 4)
            {
                MotionPoint[] mpts = App.Recording.CurrentFrame.motionPoints;
                var mp = mpts[0];
                Vector3 grav = new Vector3((float)mp.gX, (float)mp.gY, (float)mp.gZ);
                grav.Normalize();
                Vector3 basis = -Vector3.UnitY;
                Quaternion qrot = new Quaternion(Vector3.Cross(grav, basis),
                    (float)Math.Sqrt(grav.LengthSquared * basis.LengthSquared) + Vector3.Dot(grav, basis));
                Matrix4 rotMat = Matrix4.CreateFromQuaternion(qrot);
                this.alignMat = rotMat;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetTranslationX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetTranslationY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetTranslationZ"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetRotationX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetRotationY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OffsetRotationZ"));
            isDirty = true;
        }
        public override void KeyDown(wf.KeyEventArgs e)
        {
        }

        public override void KeyUp(wf.KeyEventArgs e)
        {
        }
    }
}
