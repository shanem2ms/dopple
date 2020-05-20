using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlTypes;
using OpenTK.Graphics.OpenGL;

namespace Planes
{
    class MatchesVis
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
        int frameOffset;

        public MatchesVis(int _frameOffset)
        {
            frameOffset = _frameOffset;
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
            int frameIdx = App.Recording.CurrentFrameIdx;
            OpenCV.Features features = App.OpenCV.FrameFeatures[frameIdx];

            List<Vector3> qpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> ind = new List<uint>();

            uint cIdx = 0;
            float dist = 0.002f;
            float cdist = 0.003f;
            int pIdx = 0;
            float thresh = 0.25f;
            foreach (var feature in features.features)
            {
                Vector3 color = OpenCV.Palette[(pIdx++) % 64];
                if (feature.next == null)
                    continue;
                float x = feature.pt.X;
                float y = feature.pt.Y;

                float d = feature.dist;
                if (d > thresh)
                    continue;

                d /= thresh;
                Vector3 dcolor = new Vector3(1, 0, 0) * d +
                    new Vector3(0, 0, 1) * (1 - d);

                float nx = feature.next.pt.X;
                float ny = feature.next.pt.Y;
                if (frameOffset == 1)
                {
                    x = nx;
                    y = ny;
                }

                cIdx = (uint)qpts.Count();
                qpts.Add(new Vector3(x - cdist, y - cdist, 0f));
                qpts.Add(new Vector3(x + cdist, y - cdist, 0f));
                qpts.Add(new Vector3(x - cdist, y + cdist, 0f));
                qpts.Add(new Vector3(x + cdist, y + cdist, 0f));
                colors.Add(dcolor);
                colors.Add(dcolor);
                colors.Add(dcolor);
                colors.Add(dcolor);
                ind.Add(cIdx);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 2);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 3);
                ind.Add(cIdx + 2);

                cIdx = (uint)qpts.Count();
                qpts.Add(new Vector3(x - dist, y - dist, 0f));
                qpts.Add(new Vector3(x + dist, y - dist, 0f));
                qpts.Add(new Vector3(x - dist, y + dist, 0f));
                qpts.Add(new Vector3(x + dist, y + dist, 0f));
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                ind.Add(cIdx);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 2);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 3);
                ind.Add(cIdx + 2);

                /*
                float nx = feature.next.pt.Y;
                float ny = 1 - feature.next.pt.X;
                nx = nx * 0.5f + 0.5f;
                float ldist = 0.001f;
                Vector2 dv = new Vector2(nx - x, ny - y).Normalized();
                Vector2 nv = new Vector2(dv.Y, -dv.X);
                Vector2 p = new Vector2(x, y);
                Vector2 np = new Vector2(nx, ny);
                Vector2[] pts =
                {
                    p + nv * ldist,
                    p - nv * ldist,
                    np + nv * ldist,
                    np - nv * ldist,
                };
                foreach (Vector2 pt in pts)
                { qpts.Add(new Vector3(pt.X, pt.Y, 0.5f)); }
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                ind.Add(cIdx);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 2);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 3);
                ind.Add(cIdx + 2);*/
            }

            /*
            uint cIdx = 0;
            float dist = 0.005f;
            foreach (var t in trackedList)
            {
                var feature = t.features[frameIdx - t.startFrame];                                
                cIdx = (uint)qpts.Count();
                float x = feature.pt.Y;
                float y = 1 - feature.pt.X;
                qpts.Add(new Vector3(x - dist, y - dist, 0.5f));
                qpts.Add(new Vector3(x + dist, y - dist, 0.5f));
                qpts.Add(new Vector3(x - dist, y + dist, 0.5f));
                qpts.Add(new Vector3(x + dist, y + dist, 0.5f));
                colors.Add(t.color);
                colors.Add(t.color);
                colors.Add(t.color);
                colors.Add(t.color);
                ind.Add(cIdx);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 2);
                ind.Add(cIdx + 1);
                ind.Add(cIdx + 3);
                ind.Add(cIdx + 2);
            }*/

            Vector3[] nrm = new Vector3[qpts.Count];
            for (int idx = 0; idx < nrm.Length; ++idx) nrm[idx] = new Vector3(0, 0, 1);
            genVertexArray = new VertexArray(this._Program, qpts.ToArray(), ind.ToArray(), colors.ToArray(), nrm);
        }


        public void Render(Matrix4 viewProj)
        {
            if (isDirty)
            {
                LoadVideoFrame();
                isDirty = false;
            }
            if (genVertexArray == null)
                return;
            _Program.Use(0);

            _Program.SetMat4("uMVP", ref viewProj);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(1, 1, 1));
            _Program.Set1("ambient", 1.0f);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            _Program.Set1("opacity", 1.0f);
            Matrix4 matWorldInvT = Matrix4.Identity;
            _Program.SetMat4("uWorld", ref matWorldInvT);
            _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
            _Program.SetMat4("uCamMat", ref videoMatrix);
            genVertexArray.Draw();
            GLErr.Check();
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
