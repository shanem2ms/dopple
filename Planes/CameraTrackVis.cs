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
    class CameraTrackVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        struct CamPos
        {
            public Matrix4 alignMatrix;
            public VertexArray genVertexArray;
        }

        int frameOffset;
        /// <summary>yepf
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private TextureYUV _ImageTexture;
        private Matrix4 videoMatrix;
        private Matrix4 cameraMatrix;
        CamPos[] camPositions;

        public CameraTrackVis()
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

        public void UpdateFrame(VideoFrame vf, Matrix4[] cameraPos, int frameIdx, int nframes)
        {
            this.camPositions = new CamPos[nframes];
            if (this.cameraMatrix != vf.CameraMatrix)
            {
                this.videoMatrix = vf.ProjectionMat;
                _ImageTexture.LoadImageFrame(vf.ImageWidth, vf.ImageHeight,
                    vf.imageData);
                this.cameraMatrix = vf.CameraMatrix;
            }
            for (int idx = 0; idx < nframes; ++idx)
            {
                camPositions[idx].alignMatrix = cameraPos[idx];
                camPositions[idx].genVertexArray = MakeFrust(this._Program, Matrix4.CreateScale(0.1f, 0.1f, 0.1f),
                    idx == frameIdx ? new Vector3(1,1,1) : new Vector3(0, 0, 1));
            }
        }


        public void Render(Matrix4 viewProjMat, bool overlay, bool doPick)
        {
            if (camPositions == null)
                return;

            _Program.Use(doPick ? 1 : 0);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(1, 1, 1));
            _Program.Set1("ambient", 0.5f);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            _Program.Set1("opacity", overlay ? 0.5f : 1.0f);
            _Program.Set1("ySampler", (int)0);
            _Program.Set1("uvSampler", (int)1);
            _Program.SetMat4("uCamMat", ref videoMatrix);
            _ImageTexture.BindToIndex(0, 1);
            for (int i = 0; i < camPositions.Length; ++i)
            {
                Matrix4 matWorld = camPositions[i].alignMatrix;
                Matrix4 matWVP = matWorld * viewProjMat;
                Matrix4 matWorldInvT = matWorld.Inverted();
                matWorldInvT.Transpose();
                _Program.SetMat4("uMVP", ref matWVP);
                _Program.SetMat4("uWorld", ref matWorld);
                _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
                camPositions[i].genVertexArray.Draw();
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

        public static VertexArray MakeFrust(Program program, Matrix4 mat, Vector3 color)
        {
            Vector3[] frust = _Cube.Select(v => new Vector3(v.X * (0.5f + (-v.Z + 1) * 0.5f), v.Y * (0.5f + (-v.Z + 1) * 0.5f), v.Z)).ToArray();
            frust = frust.Select(v => Vector3.TransformPosition(v, mat)).ToArray();
            ushort[] indices = new ushort[frust.Length];
            Vector3[] texCoords = new Vector3[frust.Length];
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


            Vector3[] nrmCoords = new Vector3[frust.Length];
            for (int i = 0; i < 6; ++i)
            {
                Vector3 d1 = frust[i * 6 + 1] - frust[i * 6];
                Vector3 d2 = frust[i * 6 + 2] - frust[i * 6 + 1];
                Vector3 nrm = Vector3.Cross(d1, d2).Normalized();
                for (int nIdx = 0; nIdx < 6; ++nIdx)
                {
                    nrmCoords[i * 6 + nIdx] = nrm;
                }
            }

            for (int i = 0; i < indices.Length; ++i)
            {
                indices[i] = (ushort)i;
                Vector3 xdir = xdirs[i / 12];
                Vector3 ydir = ydirs[i / 12];
                int sideIdx = i / 6;
                texCoords[i] = color;
                //new Vector3(Vector3.Dot(frust[i], xdir), Vector3.Dot(frust[i], ydir), (float)sideIdx / 6.0f);
            }

            return new VertexArray(program, frust, indices, texCoords, nrmCoords);
        }
    }

}
