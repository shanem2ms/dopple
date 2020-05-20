using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Drawing2D;

namespace Planes
{
    class MatchVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>yepf
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray genVertexArray = null;
        private Matrix4 videoMatrix;
        bool isDirty = true;

        public MatchVis()
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
            int curFrame = App.Recording.CurrentFrameIdx;

            int nextFrame = App.Recording.CurrentFrameIdx + (1 * App.FrameDelta);
            if (nextFrame >= App.Recording.Frames.Count)
                nextFrame = App.Recording.Frames.Count - 1;
            Dopple.Frame f0 = App.Recording.Frames[curFrame];
            Dopple.Frame f1 = App.Recording.Frames[nextFrame];

            Tuple<int, int>[] matches = App.ptCloudAligner.Matches;

            Dopple.VideoFrame vf0 = f0.vf;
            float invWidth = 1.0f / f0.vf.ImageWidth * f0.vf.DepthWidth;
            float invHeight = 1.0f / f0.vf.ImageHeight * f0.vf.DepthHeight;

            float dw = 1.0f / f0.vf.DepthWidth;
            float dh = 1.0f / f0.vf.DepthHeight;

            List<Vector3> mpts = new List<Vector3>();
            List<uint> mindices = new List<uint>();
            var pts0 = f0.vf.CalcDepthPoints();
            var pts1 = f1.vf.CalcDepthPoints();
            List<Vector3> qpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> ind = new List<uint>();
            float vwidth = f0.vf.ImageWidth - 1;
            float vheight = f0.vf.ImageHeight - 1;
            Matrix4 projMat = f0.vf.ProjectionMat;
            Matrix4 projInv = projMat.Inverted();
            float pixel = 1.0f / f0.vf.DepthWidth;
            foreach (var m in matches)
            {
                var p0 = pts0[m.Item1];
                Vector3 rgbcol0;
                Vector3 c0 = f0.vf.GetRGBVal((int)(p0.spt.X * vwidth), (int)(p0.spt.Y * vheight));
                rgbcol0 = c0;

                var p1 = pts1[m.Item2];
                Vector3 rgbcol1;
                Vector3 c1 = f1.vf.GetRGBVal((int)(p1.spt.X * vwidth), (int)(p1.spt.Y * vheight));
                rgbcol1 = c1;

                Vector3 spt =
                    Vector3.TransformPerspective(p0.pt, projMat);
                spt.X += pixel;
                Vector3 ptn = Vector3.TransformPerspective(spt, projInv);
                float d = (ptn - p0.pt).Length;
                float dist = d * 0.001f;

                Vector3 zdir = (p1.pt - p0.pt).Normalized();
                Vector3 xdir = Vector3.Cross(zdir, Vector3.UnitY).Normalized();
                Vector3 ydir = Vector3.Cross(xdir, zdir);

                uint cIdx = (uint)qpts.Count;
                qpts.Add(p0.pt - xdir * dist);
                qpts.Add(p0.pt + xdir * dist);
                qpts.Add(p1.pt - xdir * dist);
                qpts.Add(p1.pt + xdir * dist);

                Vector3 rgbcol = new Vector3(1, 1, 0);
                colors.Add(rgbcol);
                colors.Add(rgbcol);
                colors.Add(rgbcol);
                colors.Add(rgbcol);


                ind.Add(cIdx);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 2);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 3);
                ind.Add(cIdx + 2);
            }

            Vector3[] nrm = new Vector3[qpts.Count];
            for (int idx = 0; idx < nrm.Length; ++idx) nrm[idx] = new Vector3(0, 0, 1);
            genVertexArray = new VertexArray(this._Program, qpts.ToArray(), ind.ToArray(), colors.ToArray(), nrm);

            GLErr.Check();
        }


        public void Render(Matrix4 viewProjMat, bool doPick)
        {
            if (isDirty)
            {
                LoadVideoFrame();
                isDirty = false;
            }
            if (genVertexArray == null)
                return;

            _Program.Use(doPick ? 1 : 0);
            _Program.SetMat4("uMVP", ref viewProjMat);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(1, 1, 1));
            _Program.Set1("ambient", 1);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            _Program.Set1("opacity", 1.0f);
            Matrix4 matWorldInvT = Matrix4.Identity;
            _Program.SetMat4("uWorld", ref matWorldInvT);
            _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
            _Program.SetMat4("uCamMat", ref videoMatrix);
            genVertexArray.Draw();

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
