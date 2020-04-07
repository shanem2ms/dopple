using System;
using System.Collections.Generic;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Linq;

namespace Planes
{
    class DepthVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray genVertexArray = null;


        public DepthVis()
        {
            _Program = Program.FromFiles("Main.vert", "Main.frag");
        }

        public void Rebuild()
        {
            if (currentFrame != null)
                SetVideoFrame(currentFrame);
        }

        bool ptsMode = true;
        Dopple.Frame currentFrame;
        int triCnt;
        public void SetVideoFrame(Dopple.Frame f)
        {
            currentFrame = f;
            if (ptsMode)
            {
                Vector3[] pts = currentFrame.vf.GetDepthPoints();
                float dist = 0.001f;
                List<Vector3> qpts = new List<Vector3>();
                List<uint> ind = new List<uint>();
                foreach (Vector3 p in pts)
                {
                    if (float.IsInfinity(p.X))
                        continue;
                    uint cIdx = (uint)qpts.Count;
                    qpts.Add(p - Vector3.UnitX * dist
                               - Vector3.UnitY * dist);
                    qpts.Add(p + Vector3.UnitX * dist
                               - Vector3.UnitY * dist);
                    qpts.Add(p - Vector3.UnitX * dist
                               + Vector3.UnitY * dist);
                    qpts.Add(p + Vector3.UnitX * dist
                               + Vector3.UnitY * dist);
                    ind.Add(cIdx);
                    ind.Add(cIdx + 1);
                    ind.Add(cIdx + 2);
                    ind.Add(cIdx + 1);
                    ind.Add(cIdx + 3);
                    ind.Add(cIdx + 2);
                }

                pts = qpts.ToArray();
                genVertexArray = new VertexArray(this._Program, pts, ind.ToArray(), pts, null);
            }
            else
            {
                Vector3[] pts, texcoords;
                currentFrame.vf.MakePlanes(out pts, out texcoords, out triCnt);
                uint[] depthindices = new uint[triCnt];
                for (int idx = 0; idx < depthindices.Length; ++idx)
                    depthindices[idx] = (uint)idx;

                genVertexArray = new VertexArray(this._Program, pts, depthindices, texcoords, null);
            }
        }


        public void Render(Matrix4 viewProjMat)
        {
            if (genVertexArray == null)
                return;

            _Program.Use(0);
            _Program.SetMat4("uMVP", ref viewProjMat);
            _Program.Set3("meshColor", new Vector3(0, 0, 1));

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
