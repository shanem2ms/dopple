using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;

namespace Planes
{
    class MatchesVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        int frameOffset;
        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray matchVertexArray = null;


        public MatchesVis()
        {
            _Program = Registry.Programs["main"];
            App.Recording.OnFrameChanged += Recording_OnFrameChanged;
            LoadVideoFrame();
        }

        private void Recording_OnFrameChanged(object sender, int e)
        {
            LoadVideoFrame();
        }

        bool depthPts = true;
        int triCnt;

        Vector3[] ptColors = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0)
        };

        public void LoadVideoFrame()
        {
            List<Vector3> mpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> mindices = new List<uint>();
            float linewidth = 0.001f;
            foreach (var match in App.OpenCV.ActiveMatches)
            {
                Vector2 pt = match.pts[this.frameOffset];

                Vector3 dir = (match.wspts[1] - match.wspts[0]).Normalized();
                Vector3 cdir = Vector3.Cross(dir, Vector3.UnitY);

                uint offset = (uint)mpts.Count();
                mindices.Add(offset);
                mindices.Add(offset + 1);
                mindices.Add(offset + 2);
                mindices.Add(offset + 1);
                mindices.Add(offset + 3);
                mindices.Add(offset + 2);

                mpts.Add(match.wspts[0]
                        - cdir * linewidth);
                mpts.Add(match.wspts[0]
                        + cdir * linewidth);
                mpts.Add(match.wspts[1]
                        - cdir * linewidth);
                mpts.Add(match.wspts[1]
                        + cdir * linewidth);
                colors.Add(match.color);
                colors.Add(match.color);
                colors.Add(match.color);
                colors.Add(match.color);
            }

            Vector3[] nrm = new Vector3[mpts.Count];
            for (int idx = 0; idx < nrm.Length; ++idx) nrm[idx] = new Vector3(0, 0, 1);
            matchVertexArray = new VertexArray(this._Program, mpts.ToArray(), mindices.ToArray(), colors.ToArray(), nrm);
        }


        public void Render(Matrix4 viewProjMat)
        {
            if (matchVertexArray == null)
                return;

            _Program.Use(0);
            _Program.SetMat4("uMVP", ref viewProjMat);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(0, 0, 1));
            _Program.Set1("ambient", 1.0f);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            Matrix4 matWorldInvT = Matrix4.Identity;
            _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);

            matchVertexArray.Draw();

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
