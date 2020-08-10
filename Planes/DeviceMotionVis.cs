using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlTypes;
using OpenTK.Graphics.OpenGL;
using Dopple;
using System.Drawing.Drawing2D;

namespace Planes
{
    class DeviceMotionVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>yepf
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray []vertexArray = null;
        private Matrix4 videoMatrix;
        bool isDirty = true;

        public DeviceMotionVis()
        {
            _Program = Registry.Programs["depthpts"];
            App.Recording.OnFrameChanged += Recording_OnFrameChanged;
            App.Settings.OnSettingsChanged += Settings_OnSettingsChanged;
        }

        private void Settings_OnSettingsChanged(object sender, EventArgs e)
        {
            isDirty = true;
        }

        private void Recording_OnFrameChanged(object sender, int e)
        {
            isDirty = true;
        }


        Vector3[] ptColors = new Vector3[]
        {
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 0)
        };

        static Vector3 ConvertColor(Vector3 col)
        {
            return new Vector3(1, 1, 1);
        }

        public void LoadVideoFrame()
        {
            const int delta = 10;
            double timespan = 2.0f;
            float yScale = 0.5f;
            int startFrameIdx = Math.Max(App.Recording.CurrentFrameIdx - 10, 0);
            int endFrameIdx = Math.Min(App.Recording.NumFrames, startFrameIdx + delta * 2);

            List<MotionPoint> motionPoints = new List<MotionPoint>();

            MotionPoint[] mpts = App.Recording.Frames.GetRange(startFrameIdx, endFrameIdx - startFrameIdx).SelectMany(f => f.motionPoints)
                .OrderBy(mp => mp.timeStamp).ToArray();

            if (mpts.Length == 0)
                return;

            double startTime = mpts[0].timeStamp;
            List<Vector4>[] rList = new List<Vector4>[3] { new List<Vector4>(), new List<Vector4>(), new List<Vector4>() };
            foreach (MotionPoint mp in mpts)
            {
                double xval0 = ((mp.timeStamp - startTime) / timespan);
                xval0 = xval0 * 2 - 1;
                Vector4 pt = new Vector4((float)xval0, (float)mp.rX * yScale,
                    (float)mp.rY * yScale, (float)mp.rZ * yScale);
                rList[0].Add(pt);

                Quaterniond q = new Quaterniond(mp.qX, mp.qY, mp.qZ, mp.qW);                

                Vector4 pt2 = new Vector4((float)xval0, (float)q.X * yScale,
                    (float)q.Y * yScale, (float)q.Z * yScale);
                rList[1].Add(pt2);

                Vector4 pt3 = new Vector4((float)xval0, (float)mp.gX * yScale,
                    (float)mp.gY * yScale, (float)mp.gZ * yScale);
                rList[2].Add(pt3);
            }

            vertexArray = new VertexArray[rList.Length];
            for (int i = 0; i < rList.Length; ++i)
            {
                List<Vector4> rL = rList[i];

                List<Vector3> qpts = new List<Vector3>();
                List<Vector3> colors = new List<Vector3>();
                List<uint> ind = new List<uint>();
                Vector3 xRCol = new Vector3(1, 0, 0);
                Vector3 yRCol = new Vector3(0, 1, 0);
                Vector3 zRCol = new Vector3(0, 0, 1);

                for (int idx = 0; idx < rL.Count - 1; ++idx)
                {
                    DrawLine(new Vector3(rL[idx].X, rL[idx].Y, 0),
                        new Vector3(rL[idx + 1].X, rL[idx + 1].Y, 0),
                        0.005f, xRCol, qpts, ind, colors);

                    DrawLine(new Vector3(rL[idx].X, rL[idx].Z, 0),
                        new Vector3(rL[idx + 1].X, rL[idx + 1].Z, 0),
                        0.005f, yRCol, qpts, ind, colors);

                    DrawLine(new Vector3(rL[idx].X, rL[idx].W, 0),
                        new Vector3(rL[idx + 1].X, rL[idx + 1].W, 0),
                        0.005f, zRCol, qpts, ind, colors);
                }

                Vector3[] nrm = new Vector3[qpts.Count];
                for (int idx = 0; idx < nrm.Length; ++idx) nrm[idx] = new Vector3(0, 0, 1);
                vertexArray[i] = new VertexArray(this._Program, qpts.ToArray(), ind.ToArray(), colors.ToArray(), nrm);
            }
        }

        void DrawLine(Vector3 pt0, Vector3 pt1, float width, Vector3 color, List<Vector3> pts, List<uint> ind,
            List<Vector3> colors
            )
        {
            uint startIdx = (uint)pts.Count;
            Vector3 dir = (pt1 - pt0).Normalized();
            Vector3 nrm = Vector3.Cross(dir, Vector3.UnitZ);
            pts.Add(pt0 - nrm * width);
            pts.Add(pt0 + nrm * width);
            pts.Add(pt1 - nrm * width);
            pts.Add(pt1 + nrm * width);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            ind.Add(startIdx);
            ind.Add(startIdx + 1);
            ind.Add(startIdx + 2);
            ind.Add(startIdx + 1);
            ind.Add(startIdx + 3);
            ind.Add(startIdx + 2);
        }


        public void Render(Matrix4 viewProj)
        {
            if (isDirty)
            {
                LoadVideoFrame();
                isDirty = false;
            }
            if (vertexArray == null)
                return;
            _Program.Use(0);

            Vector3 scl = new Vector3(1, 0.5f, 1);
            Matrix4[] mvp = { Matrix4.CreateScale(scl) * Matrix4.CreateTranslation(new Vector3(0.5f, -0.5f, 0)),
                Matrix4.CreateScale(scl) * Matrix4.CreateTranslation(new Vector3(0.5f, 0, 0)),
                Matrix4.CreateScale(scl) * Matrix4.CreateTranslation(new Vector3(0.5f, 0.5f, 0)) };

            for (int i = 0; i < vertexArray.Length; ++i)
            {
                _Program.SetMat4("uMVP", ref mvp[i]);
                _Program.Set1("opacity", 1.0f);
                _Program.Set3("meshColor", new Vector3(1, 1, 1));
                _Program.Set1("ambient", 1.0f);
                _Program.Set3("lightPos", new Vector3(2, 5, 2));
                _Program.Set1("opacity", 1.0f);
                Matrix4 matWorldInvT = Matrix4.Identity;
                _Program.SetMat4("uWorld", ref matWorldInvT);
                _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
                _Program.SetMat4("uCamMat", ref videoMatrix);
                vertexArray[i].Draw();
                GLErr.Check();
            }
        }
        private static readonly Vector3[] _Quad = new Vector3[] {
            new Vector3(1.0f, 0.0f, 0.0f),  // 0 
            new Vector3(0.0f, 0.0f, 0.0f),  // 1
            new Vector3(0.0f, 1.0f, 0.0f),  // 2

            new Vector3(1.0f, 0.0f, 0.0f),  // 0 
            new Vector3(0.0f, 1.0f, 0.0f),  // 2
            new Vector3(1.0f, 1.0f, 0.0f)  // 3 
        };

        private static readonly uint[] _Indices = new uint[]
        {
            0,1,2,3,4,5
        };


        private static readonly Vector3[] _TexCoords = new Vector3[] {
            new Vector3(0.0f, 1.0f, 0.0f),  // 0 
            new Vector3(1.0f, 1.0f, 0.0f),  // 1
            new Vector3(1.0f, 0.0f, 0.0f),  // 2

            new Vector3(0.0f, 1.0f, 0.0f),  // 0 
            new Vector3(1.0f, 0.0f, 0.0f),  // 2
            new Vector3(0.0f, 0.0f, 0.0f)  // 3 
        };

    }
}
