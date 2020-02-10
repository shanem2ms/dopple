using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;

namespace Dopple
{
    public class TwoDPointVis
    {
        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray[] vaMeshes = null;
        PtCldAlignNative currentAlign;
        bool hasNewMesh = false;
        public PtCldAlignNative CurrentAlign
        {
            get { return this.currentAlign; }
            set { this.currentAlign = value; hasNewMesh = true; }
        }

        public TwoDPointVis()
        {
            _Program = Program.FromFiles("TwoDPointVis.vert", "TwoDPointVis.frag");
            CreatePoints();
        }
        public bool ApplyAlignTransform { get; set; } = true;

        int[] indexCnt = new int[2];

        Vector3[] ptsStart;
        Vector3[] ptsEnd;
        public Vector3 bestFit;
        void CreatePoints()
        {
            List<Vector3> pts = new List<Vector3>();
            Random r = new Random();
            for (int i = 0; i < 8; ++i)
            {
                pts.Add(new Vector3((float)(r.NextDouble() - 0.5f),
                    (float)(r.NextDouble() - 0.5f),
                    0.5f));
            }
            ptsStart = pts.ToArray();
            List<Vector3> tpts = new List<Vector3>();
            float rotvariance = 10;
            float transvariance = 0.2f;
            float offsetx = 0.1f;
            float offsety = 0.2f;
            float rotoffset = 20.0f;
            foreach (Vector3 pt in pts)
            {
                float rot = (rotoffset + (float)r.NextDouble() * rotvariance) * (float)Math.PI / 180.0f;
                float sint = (float)Math.Sin(rot);
                float cost = (float)Math.Cos(rot);
                Matrix4 rotmat = Matrix4.CreateRotationZ(rot);
                Matrix4 tmat = Matrix4.CreateTranslation(offsetx + transvariance, offsety + transvariance, 0);
                Vector3 v0 = Vector3.TransformPosition(pt, tmat * rotmat);
                tpts.Add(v0); 
            }

            ptsEnd = tpts.ToArray();

            this.bestFit = BestFit(1.0f);
        }

        void LoadVAS()
        {
            vaMeshes = new VertexArray[2];
            LoadVA(ptsStart, out vaMeshes[0]);
            LoadVA(ptsEnd, out vaMeshes[1]);
        }

        void LoadVA(Vector3 []pts, out VertexArray va)
        {
            float cubesize = 0.01f;
            List<ushort> indices = new List<ushort>();
            Vector3[] ptoffsets = new Vector3[4]
            {
                new Vector3(-cubesize, -cubesize, 0),
                new Vector3(cubesize, -cubesize, 0),
                new Vector3(cubesize, cubesize, 0),
                new Vector3(-cubesize, cubesize, 0)
            };

            List<Vector3> cpts = new List<Vector3>();
            foreach (Vector3 pt in pts)
            {
                for (int o = 0; o < 4; ++o)
                {
                    cpts.Add(pt + ptoffsets[o]);
                }
                int offset = cpts.Count;
                indices.Add((ushort)offset);
                indices.Add((ushort)(offset + 1));
                indices.Add((ushort)(offset + 2));
                indices.Add((ushort)offset);
                indices.Add((ushort)(offset + 2));
                indices.Add((ushort)(offset + 3));
            }
            va = new VertexArray(_Program, cpts.ToArray(), indices.ToArray(), null, null);
        }

        static float sqrt(float val)
        {
            return (float)System.Math.Sqrt((double)val);
        }
        static float pow(float val, float pow)
        {
            return (float)System.Math.Pow((double)val, (double)pow);
        }

        static float itan(float val)
        {
            return (float)System.Math.Atan(val);
        }

        public Vector3 BestFit(float mul)
        {
            float twoDAngle = 0.0f;
            Vector2 trans = Vector2.Zero;
            Vector3 score;
            for (int i = 0; i < 100; ++i)
            {
                score = GetScore(twoDAngle, trans);
                System.Diagnostics.Debug.WriteLine($"{i} = {score.Length}");
                if (score.Length < 1e-5)
                    break;
                twoDAngle -= score.X * mul;
                trans.X -= score.Y * 0.5f;
                trans.Y -= score.Z * 0.5f;
            }

            return new Vector3(twoDAngle, trans.X, trans.Y);
        }

        public Vector3 GetScore(float twoDAngle,
            Vector2 translate)
        {
            float tx = translate.X;
            float ty = translate.Y;
            float totalScoreR = 0;
            float totalScoreX = 0;
            float totalScoreY = 0;
            // https://www.wolframalpha.com/input/?i=d%2Fdt+(((a+-+(x+Cos(t)+-+y+Sin(t)))%5E2)+%2B+((b+-+(x+Sin(t)+%2B+y+Cos(t)))%5E2))
            // d/dt (((a - (x Cos(t) - y Sin(t)))^2) + ((b - (x Sin(t) + y Cos(t)))^2))
            // 0 = 2 ((-b x + a y) cos(t) + (a x + b y) sin(t))
            // t = 2 tan^(-1)((a x + b y - sqrt((a^2 + b^2) (x^2 + y^2)))/(b x - a y))

            // 2 ((ty x0 - tx y0 + x1 y0 - x0 y1) Cos[t] +(-tx x0 + x0 x1 - ty y0 + y0 y1) Sin[t])

            // 2 (x_src (ty cosr - tx sinr + sinr x_dest - cosr y_dest) +(-tx cosr - ty sinr + cosr x_dest + sinr y_dest) y_src)
            // 2 (tx - x_dest[[n]] + cosr x_src[[n]] - y_src[[n]] sinr)
            // 2 (ty - y_dest[[n]] + cosr y_src[[n]] +  x_src[[n]] sinr)


            // (2 * (-ty + y_dest - cosr * y_src - x_src * sinr) * (-cosr * x_src + y_src * sinr) + 2 * (cosr * y_src + x_src * sinr) * (-tx + x_dest - cosr * x_src +y_src * sinr))
            // -2 * (-tx + x_dest - cosr * x_src + y_src * sinr)
            // -2 * (-ty + y_dest - cosr * y_src - x_src * sinr)
            for (int i = 0; i < this.ptsStart.Length; ++i)
            {
                float cosr = (float)Math.Cos(twoDAngle);
                float sinr = (float)Math.Sin(twoDAngle);
                float x_src = this.ptsEnd[i].X;
                float y_src = this.ptsEnd[i].Y;
                float x_dest = this.ptsStart[i].X;
                float y_dest = this.ptsStart[i].Y;

                float derivt =
                    (2 * (-ty + y_dest - cosr * y_src - x_src * sinr) * (-cosr * x_src + y_src * sinr) + 2 * (cosr * y_src + x_src * sinr) * (-tx + x_dest - cosr * x_src + y_src * sinr));
                float derivx =
                    -2 * (-tx + x_dest - cosr * x_src + y_src * sinr);
                float derivy =
                    -2 * (-ty + y_dest - cosr * y_src - x_src * sinr);

                totalScoreR += derivt;
                totalScoreX += derivx;
                totalScoreY += derivy;
            }
            float invlen = 1.0f / this.ptsStart.Length;
            return new Vector3(totalScoreR * invlen, totalScoreX * invlen, totalScoreY * invlen);
        }
        public void Render(float twoDAngle, Vector2 twoDTranslate)
        {
            if (vaMeshes == null)
                LoadVAS();
            if (vaMeshes == null)
                return;

            // Select the program for drawing
            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);
            // Compute the model-view-projection on CPU

            for (int idx = 0; idx < 2; ++idx)
            {
                Matrix4 matWorldViewProj = idx == 0 ? Matrix4.Identity : Matrix4.CreateRotationZ(twoDAngle) *
                    Matrix4.CreateTranslation(twoDTranslate.X, twoDTranslate.Y, 0);
                GL.UniformMatrix4(_Program.LocationMVP, false, ref matWorldViewProj);

                _Program.Set3("meshColor", idx == 0 ? new Vector3(0, 0, 1) : new Vector3(1, 1, 0));

                vaMeshes[idx].Draw();
            }
        }

        public void Dispose()
        {
            _Program?.Dispose();
            foreach (VertexArray vaMesh in vaMeshes)
            {
                vaMesh.Dispose();
            }
        }
    }
}
