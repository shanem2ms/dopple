using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Drawing2D;
using Dopple;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Planes
{
    class WorldVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        int frameOffset;
        /// <summary>yepf
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray genVertexArray = null;
        private TextureYUV _ImageTexture;
        private Matrix4 videoMatrix;

        public WorldVis()
        {
            _ImageTexture = new TextureYUV();
            _Program = Registry.Programs["depthpts"];
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

        public void UpdateFrame(Vector3 []worldPts)
        {
            float qscl = 1;
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector3> tex = new List<Vector3>();
            List<uint> ind = new List<uint>();
            foreach (var p in worldPts)
            {
                if (p == null)
                    continue;

                if (float.IsInfinity(p.X))
                    continue;

                AddCube(pos, tex, nrm, ind, 0.01f, p);
            }
            genVertexArray = 
                new VertexArray(this._Program, pos.ToArray(), ind.ToArray(), tex.ToArray(), nrm.ToArray());
        }


        public void Render(Matrix4 viewProjMat, bool overlay, bool doPick)
        {
            if (genVertexArray == null)
                return;

            _Program.Use(doPick ? 1 : 0);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(1, 1, 1));
            _Program.Set1("ambient", 1.0f);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            _Program.Set1("opacity", overlay ? 0.5f : 1.0f);
            _Program.Set1("ySampler", (int)0);
            _Program.Set1("uvSampler", (int)1);
            _Program.SetMat4("uCamMat", ref videoMatrix);
            _ImageTexture.BindToIndex(0, 1);
            Matrix4 matWorld = Matrix4.Identity;
            Matrix4 matWVP = matWorld * viewProjMat;
            Matrix4 matWorldInvT = matWorld.Inverted();
            matWorldInvT.Transpose();
            _Program.SetMat4("uMVP", ref matWVP);
            _Program.SetMat4("uWorld", ref matWorld);
            _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
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


        private static readonly Vector3[] _Cube = new Vector3[] {
            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(1.0f, -1.0f, -1.0f),  // 1
            new Vector3(1.0f, 1.0f, -1.0f),  // 2

            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(1.0f, 1.0f, -1.0f),  // 2
            new Vector3(-1.0f, 1.0f, -1.0f),  // 3

            new Vector3(-1.0f, -1.0f, 1.0f),  // 4
            new Vector3(1.0f, -1.0f, 1.0f),  // 5
            new Vector3(1.0f, 1.0f, 1.0f),  // 6

            new Vector3(-1.0f, -1.0f, 1.0f),  // 4
            new Vector3(1.0f, 1.0f, 1.0f),  // 6
            new Vector3(-1.0f, 1.0f, 1.0f),  // 7

            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(1.0f, -1.0f, -1.0f),  // 1
            new Vector3(1.0f, -1.0f, 1.0f),  // 5

            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(1.0f, -1.0f, 1.0f),  // 5
            new Vector3(-1.0f, -1.0f, 1.0f),  // 4

            new Vector3(1.0f, 1.0f, -1.0f),  // 2
            new Vector3(-1.0f, 1.0f, -1.0f),  // 3
            new Vector3(-1.0f, 1.0f, 1.0f),  // 7

            new Vector3(1.0f, 1.0f, -1.0f),  // 2
            new Vector3(-1.0f, 1.0f, 1.0f),  // 7
            new Vector3(1.0f, 1.0f, 1.0f),  // 6

            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(-1.0f, 1.0f, -1.0f),  // 3
            new Vector3(-1.0f, 1.0f, 1.0f),  // 7

            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(-1.0f, 1.0f, 1.0f),  // 7
            new Vector3(-1.0f, -1.0f, 1.0f),  // 4

            new Vector3(1.0f, -1.0f, -1.0f),  // 1
            new Vector3(1.0f, 1.0f, -1.0f),  // 2
            new Vector3(1.0f, 1.0f, 1.0f),  // 6

            new Vector3(1.0f, -1.0f, -1.0f),  // 1
            new Vector3(1.0f, 1.0f, 1.0f),  // 6
            new Vector3(1.0f, -1.0f, 1.0f),  // 5
        };

        static void AddCube(List<Vector3> pos, List<Vector3> texs, List<Vector3> nrms, List<uint> indices, float scale, Vector3 offset)
        {
            Vector3[] normals = new Vector3[3]
            {
                Vector3.UnitZ,
                Vector3.UnitY,
                Vector3.UnitX
            };
            Vector3[] xdirs = new Vector3[3]
            {
                Vector3.UnitX,
                Vector3.UnitX,
                Vector3.UnitZ
            };
            Vector3[] ydirs = new Vector3[3]
            {
                Vector3.UnitY,
                Vector3.UnitZ,
                Vector3.UnitY
            };


            int iOffset = pos.Count;

            Vector3 []c2 = _Cube.Select(cv => cv * scale + offset).ToArray();
            pos.AddRange(c2);

            Vector3[] nrmCoords = new Vector3[c2.Length];
            for (int i = 0; i < 6; ++i)
            {
                Vector3 d1 = c2[i * 6 + 1] - c2[i * 6];
                Vector3 d2 = c2[i * 6 + 2] - c2[i * 6 + 1];
                Vector3 nrm = Vector3.Cross(d1, d2).Normalized();
                for (int nIdx = 0; nIdx < 6; ++nIdx)
                {
                    nrmCoords[i * 6 + nIdx] = nrm;
                }
            }

            nrms.AddRange(nrmCoords);

            Vector3[] texCoords = new Vector3[c2.Length];
            for (int i = 0; i < c2.Length; ++i)
            {
                indices.Add((uint)(i + iOffset));
                Vector3 xdir = xdirs[i / 12];
                Vector3 ydir = ydirs[i / 12];
                int sideIdx = i / 6;
                texCoords[i] = new Vector3(Vector3.Dot(c2[i], xdir),
                    Vector3.Dot(c2[i], ydir), (float)sideIdx / 6.0f);
            }
            texs.AddRange(texCoords);
        }


    }
}
