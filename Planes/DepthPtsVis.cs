using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Drawing2D;
using Dopple;

namespace Planes
{
    class DepthPtsVis
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

        public DepthPtsVis(int _frameOffset)
        {
            frameOffset = _frameOffset;
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

        public void UpdateFrame(VideoFrame vf, NrmPt []ptArray, Vector3 tint)
        {
            this.videoMatrix = vf.ProjectionMat;
            _ImageTexture.LoadImageFrame(vf.ImageWidth, vf.ImageHeight,
                vf.imageData);

            List<Vector3> mpts = new List<Vector3>();
            List<uint> mindices = new List<uint>();
            List<Vector3> qpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> ind = new List<uint>();
            float vwidth = vf.ImageWidth - 1;
            float vheight = vf.ImageHeight - 1;
            Matrix4 projMat = vf.CameraMatrix;
            Matrix4 projInv = projMat.Inverted();
            int dw = vf.DepthWidth;
            int dh = vf.DepthHeight;
            float pixelX = 1.0f / dw;
            float pixelY = 1.0f / dh;
            pixelX = pixelX * pixelX;
            pixelY = pixelY * pixelY;
            foreach (var p in ptArray)
            {
                if (p == null)
                    continue;

                if (float.IsInfinity(p.pt.X))
                    continue;

                Vector3 rgbcol;
                Vector3 c = vf.GetRGBVal((int)(p.spt.X * vwidth), (int)(p.spt.Y * vheight));
                rgbcol = c;

                Vector3 spt =
                    Vector3.TransformPerspective(p.pt, projMat);
                spt.X += pixelX;
                spt.Y += pixelY;
                Vector3 ptn = Vector3.TransformPerspective(spt, projInv);
                float dx = Math.Abs((ptn - p.pt).X);
                float dy = Math.Abs((ptn - p.pt).Y);

                Vector3 ux, uy;
                if (Math.Abs(p.nrm.X) > Math.Abs(p.nrm.Y))
                {
                    ux = Vector3.Cross(Vector3.UnitY, p.nrm);
                    uy = Vector3.Cross(ux, p.nrm);
                }
                else
                {
                    uy = Vector3.Cross(Vector3.UnitX, p.nrm);
                    ux = Vector3.Cross(uy, p.nrm);
                }

                uint cIdx = (uint)qpts.Count;
                qpts.Add(p.pt - ux * dx
                            - uy * dy);
                qpts.Add(p.pt + ux * dx
                            - uy * dy);
                qpts.Add(p.pt - ux * dx
                            + uy * dy);
                qpts.Add(p.pt + ux * dx
                            + uy * dy);

                rgbcol += tint * 0.35f;

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
        }


        public void Render(Matrix4 viewProjMat, bool overlay, bool doPick)
        {
            if (genVertexArray == null)
                return;

            _Program.Use(doPick ? 1 : 0);
            _Program.SetMat4("uMVP", ref viewProjMat);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(1, 1, 1));
            _Program.Set1("ambient", 1.0f);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            _Program.Set1("opacity", 1.0f);
            Matrix4 matWorldInvT = Matrix4.Identity;
            _Program.SetMat4("uWorld", ref matWorldInvT);
            _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
            _Program.SetMat4("uCamMat", ref videoMatrix);
            _Program.Set1("ySampler", (int)0);
            _Program.Set1("uvSampler", (int)1);
            _ImageTexture.BindToIndex(0, 1);
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
