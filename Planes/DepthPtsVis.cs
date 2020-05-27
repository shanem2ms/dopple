using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Drawing2D;

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
        private VertexArray matchVertexArray = null;
        private TextureYUV _ImageTexture;
        private Matrix4 videoMatrix;
        bool isDirty = true;

        public DepthPtsVis(int _frameOffset, bool _depthPts)
        {
            this.depthPts = _depthPts;
            frameOffset = _frameOffset;
            _ImageTexture = new TextureYUV();
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

        bool depthPts = false;

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
            int curFrame = App.Recording.CurrentFrameIdx + (this.frameOffset * App.FrameDelta);
            if (curFrame >= App.Recording.Frames.Count)
                curFrame = App.Recording.Frames.Count - 1;
            Dopple.Frame f = App.Recording.Frames[curFrame];

            bool firstFrame = this.frameOffset == 0;

            this.videoMatrix = f.vf.ProjectionMat;
            Dopple.VideoFrame vf = f.vf;
            _ImageTexture.LoadImageFrame(vf.ImageWidth, vf.ImageHeight,
                vf.imageData);

            if (depthPts)
            {
                float invWidth = 1.0f / f.vf.ImageWidth * f.vf.DepthWidth;
                float invHeight = 1.0f / f.vf.ImageHeight * f.vf.DepthHeight;

                float dw = 1.0f / f.vf.DepthWidth;
                float dh = 1.0f / f.vf.DepthHeight; 

                List<Vector3> mpts = new List<Vector3>();
                List<uint> mindices = new List<uint>();
                var pts = f.vf.CalcDepthPoints();
                List<Vector3> qpts = new List<Vector3>();
                List<Vector3> colors = new List<Vector3>();
                List<uint> ind = new List<uint>();
                float vwidth = f.vf.ImageWidth - 1;
                float vheight = f.vf.ImageHeight - 1;
                Matrix4 projMat = f.vf.ProjectionMat;
                Matrix4 projInv = projMat.Inverted();
                float pixel = 1.0f / f.vf.DepthWidth;
                foreach (var kv in pts)
                {
                    var p = kv.Value;
                    if (float.IsInfinity(p.pt.X))
                        continue;

                    Vector3 rgbcol;
                    Vector3 c = f.vf.GetRGBVal((int)(p.spt.X * vwidth), (int)(p.spt.Y * vheight));
                    rgbcol = c;

                    Vector3 spt =
                        Vector3.TransformPerspective(p.pt, projMat);
                    spt.X += pixel;
                    Vector3 ptn = Vector3.TransformPerspective(spt, projInv);
                    float d = (ptn - p.pt).Length;
                    float dist = d * 0.0025f;

                    uint cIdx = (uint)qpts.Count;
                    qpts.Add(p.pt - Vector3.UnitX * dist
                               - Vector3.UnitY * dist);
                    qpts.Add(p.pt + Vector3.UnitX * dist
                               - Vector3.UnitY * dist);
                    qpts.Add(p.pt - Vector3.UnitX * dist
                               + Vector3.UnitY * dist);
                    qpts.Add(p.pt + Vector3.UnitX * dist
                               + Vector3.UnitY * dist);

                    rgbcol += (firstFrame ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1)) * 0.35f;

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
                matchVertexArray = new VertexArray(this._Program, mpts.ToArray(), mindices.ToArray(), null, null);
            }
            else
            {
                Vector3[] pts, texcoords, nrms;

                int vtxcnt;
                f.vf.MakePlanes(out pts, out texcoords, out vtxcnt);
                uint[] depthindices = new uint[vtxcnt];
                nrms = new Vector3[vtxcnt];
                for (int idx = 0; idx < depthindices.Length; idx++)
                {
                    texcoords[idx] = ConvertColor(texcoords[idx]);
                    depthindices[idx] = (uint)idx;
                }
                for (int idx = 0; idx < depthindices.Length; idx += 3)
                {
                    Vector3 v1 = pts[idx + 1] - pts[idx];
                    Vector3 v2 = pts[idx + 2] - pts[idx + 1];
                    Vector3 nrm = Vector3.Cross(v1, v2).Normalized();
                    nrms[idx] = nrm;
                    nrms[idx + 1] = nrm;
                    nrms[idx + 2] = nrm;
                }

                genVertexArray = new VertexArray(this._Program, pts, depthindices, texcoords, nrms);
            }
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
            _Program.Set1("ambient", this.depthPts ? 1 : 0.4f);
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
