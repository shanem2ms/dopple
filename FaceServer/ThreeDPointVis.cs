using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;

namespace Dopple
{
    public class ThreeDPointVis
    {
        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray vaMesh = null;
        PtCldAlignNative currentAlign;
        public PtCldAlignNative CurrentAlign
        {
            get { return this.currentAlign; }
            set { this.currentAlign = value; }
        }

        public ThreeDPointVis()
        {
            _Program = Program.FromFiles("TwoDPointVis.vert", "TwoDPointVis.frag");
            //CreatePoints();
        }
        public bool ApplyAlignTransform { get; set; } = true;
        public Matrix4? OverrideTransform { get; set; } = null;

        int[] indexCnt = new int[2];

        Vector3[] ptsStart;
        Vector3[] ptsEnd;
        public Vector4 bestFitRot = new Vector4(Vector3.UnitZ, 0);
        public Vector3 bestFitTrans = Vector3.Zero;
        int numSteps = 0;
        public void LoadPoints(Vector3[]pts0, Vector3 []pts1)
        {
            this.ptsStart = pts0;
            this.ptsEnd = pts1;
        }

        Matrix4 rotmat;
        void CreatePoints()
        {
            List<Vector3> pts = new List<Vector3>();
            Random r = new Random();
            for (int i = 0; i < 100; ++i)
            {
                pts.Add(new Vector3((float)(r.NextDouble() - 0.5f),
                    (float)(r.NextDouble() - 0.5f),
                    (float)(r.NextDouble() - 0.5f)));
            }
            ptsStart = pts.ToArray();
            List<Vector3> tpts = new List<Vector3>();
            float angle = 30.0f * (float)Math.PI / 180.0f;
            Quaternion quatRot = Quaternion.FromAxisAngle(Vector3.UnitY, angle);
            rotmat = Matrix4.CreateFromQuaternion(quatRot);

            foreach (Vector3 pt in pts)
            {
                Vector3 v0 = Vector3.TransformPosition(pt, rotmat);
                tpts.Add(v0);
            }

            //this.bestFitRot = new Vector4(Vector3.UnitY, -30.0f * (float)Math.PI / 180.0f);
            ptsEnd = tpts.ToArray();

            FitPoints();
        }

        void FitPoints()
        {
            Matrix4 mat0 = Matrix4.Zero;
            mat0.Row0 = new Vector4(this.ptsStart[0], 1);
            mat0.Row1 = new Vector4(this.ptsStart[1], 1);
            mat0.Row2 = new Vector4(this.ptsStart[2], 1);
            mat0.Row3 = Vector4.UnitW;
            mat0.Transpose();
            Matrix4 mat1 = Matrix4.Zero;
            mat1.Row0 = new Vector4(this.ptsEnd[0], 1);
            mat1.Row1 = new Vector4(this.ptsEnd[1], 1);
            mat1.Row2 = new Vector4(this.ptsEnd[2], 1);
            mat1.Row3 = Vector4.UnitW;
            mat1.Transpose();
            Matrix4 rot = Matrix4.Mult(mat0, mat1.Inverted());
        }
        void LoadVAS(Matrix4 transform)
        {
            if (ptsStart == null)
            {
                this.vaMesh = null;
                return;
            }
            Visual v = GetConnectedLine(ptsStart, ptsEnd, transform);
            this.vaMesh = new VertexArray(_Program, v.pos, v.indices, v.texcoord, null);
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

        public float VisScale = 0.005f;
        public Visual GetConnectedLine(Vector3[] pts0, Vector3[] pts1, Matrix4 worldTrans)
        {
            List<Vector3> pts = new List<Vector3>();
            List<Vector3> col = new List<Vector3>();
            List<uint> indices = new List<uint>();
            float scale = VisScale;
            Vector3[] colors = new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            for (int ptIdx = 0; ptIdx < pts0.Length; ++ptIdx)
            {
                Vector3[] ctr = new Vector3[] { pts0[ptIdx],
                    Vector3.TransformPosition(pts1[ptIdx], worldTrans) };
                for (int cIdx = 0; cIdx < 2; ++cIdx)
                {
                    Matrix4 mat = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(ctr[cIdx]);
                    uint startIdx = (uint)pts.Count;
                    indices.AddRange(_CubeIndices.Select(a => a + startIdx));
                }

                {
                    Vector3 ctravg = (ctr[1] + ctr[0]) * 0.5f;
                    Vector3 wdir = (ctr[1] - ctr[0]).Normalized();
                    Vector3 udir = Vector3.Cross(wdir, Vector3.UnitY).Normalized();
                    Vector3 vdir = Vector3.Cross(udir, wdir).Normalized();
                    float len = (ctr[1] - ctr[0]).Length * 0.5f;
                    wdir *= len;
                    udir *= scale * 0.5f;
                    vdir *= scale * 0.5f;

                    uint startIdx = (uint)pts.Count;
                    for (int idx = 0; idx < _Cube.Length; ++idx)
                    {
                        Vector3 cpos = _Cube[idx].X * udir +
                        _Cube[idx].Y * vdir +
                        _Cube[idx].Z * wdir;
                        cpos += ctravg;
                        pts.Add(cpos);
                        col.Add(new Vector3(1, 0, 1));
                    }
                    indices.AddRange(_CubeIndices.Select(a => a + startIdx));
                }
            }

            Visual v = new Visual();
            v.texcoord = col.ToArray();
            v.pos = pts.ToArray();
            v.indices = indices.ToArray();
            return v;
        }
        public void SetBestFit(Vector4 q, Vector3 trans)
        {
            this.bestFitTrans = trans;
            this.bestFitRot = q;
            this.numSteps = 0;
        }
        static float pow2(float x)
        { return x * x;  }
        public void Render(Matrix4 worldMat, Matrix4 viewProj)
        {
            Matrix4 mat = OverrideTransform.HasValue ? OverrideTransform.Value :
                worldMat;
            LoadVAS(mat);
            if (vaMesh == null)
                return;

            // Select the program for drawing
            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);
            // Compute the model-view-projection on CPU

            GL.UniformMatrix4(_Program.LocationMVP, false, ref viewProj);

            GL.Disable(EnableCap.DepthTest);
            vaMesh.Draw();
            GL.Enable(EnableCap.DepthTest);
        }

        public void Dispose()
        {
            _Program?.Dispose();
            vaMesh.Dispose();
        }

        private static readonly Vector3[] _Cube = new Vector3[] {
            new Vector3(-1.0f, -1.0f, -1.0f),  // 0 
            new Vector3(1.0f, -1.0f, -1.0f),  // 1
            new Vector3(1.0f, 1.0f, -1.0f),  // 2
            new Vector3(-1.0f, 1.0f, -1.0f),  // 3
            new Vector3(-1.0f, -1.0f, 1.0f),  // 4
            new Vector3(1.0f, -1.0f, 1.0f),  // 5
            new Vector3(1.0f, 1.0f, 1.0f),  // 6
            new Vector3(-1.0f, 1.0f, 1.0f),  // 7
        };

        private static readonly uint[] _CubeIndices = new uint[]
        {
            0, 1, 2,
            0, 2, 3,
            4, 5, 6,
            4, 6, 7,

            0, 1, 5,
            0, 5, 4,
            2, 3, 7,
            2, 7, 6,

            0, 3, 7,
            0, 7, 4,
            1, 2, 6,
            1, 6, 5
        };
    }
}

