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
    class MatchVis
    {
        Aligner aligner = new Aligner();

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

        public void UpdateFrame(VideoFrame vf0, NrmPt[][] ptArrays, int[] matches)
        {
            List<Vector3> qpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> ind = new List<uint>();

            for (int idx = 0; idx < matches.Length; idx += 2)
            {
                var p0n = ptArrays[0][matches[idx]];
                var p1n = ptArrays[1][matches[idx + 1]];
                var p0 = p0n.pt;

                Vector3 nrmDist = Vector3.Dot((p1n.pt - p0n.pt), p0n.nrm.Normalized()) * p0n.nrm;
                var p1 = p0n.pt + nrmDist;

                //var p1 = p1n.pt;

                Matrix4 projMat = vf0.CameraMatrix;
                Matrix4 projInv = projMat.Inverted();

                Vector3 spt =
                    Vector3.TransformPerspective(p0, projMat);
                float pixelX = 1.0f / vf0.DepthWidth;
                float pixelY = 1.0f / vf0.DepthHeight;
                pixelX *= 0.25f;

                Vector3 ptn = Vector3.TransformPerspective(spt, projInv);
                Vector3 zdir = (p1 - p0).Normalized();
                Vector3 xdir = Vector3.Cross(zdir, Vector3.UnitY).Normalized();
                Vector3 ydir = Vector3.Cross(xdir, zdir);

                uint cIdx = (uint)qpts.Count;
                qpts.Add(p0 - xdir * pixelX);
                qpts.Add(p0 + xdir * pixelX);
                qpts.Add(p1 - xdir * pixelX);
                qpts.Add(p1 + xdir * pixelX);

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
