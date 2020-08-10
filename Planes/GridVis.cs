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
    class GridVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>yepf
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray genVertexArray = null;
        bool isDirty = true;
        public GridVis()
        {
            _Program = Registry.Programs["depthpts"];
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

        public void Update()
        {
            List<Vector3> qpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> ind = new List<uint>();
            int GS = 10;
            uint cIdx = 0;
            float w = 0.01f;
            int pIdx = 0;
            float F = -2;
            for (int x = -GS; x < GS; ++x)
            {
                Vector3 color = new Vector3(0, 0, 1);
                cIdx = (uint)qpts.Count();
                qpts.Add(new Vector3(x - w, F, -GS));
                qpts.Add(new Vector3(x + w, F, -GS));
                qpts.Add(new Vector3(x - w, F, GS));
                qpts.Add(new Vector3(x + w, F, GS));
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
            }
            for (int z = -GS; z < GS; ++z)
            {
                Vector3 color = new Vector3(0, 0, 1);
                cIdx = (uint)qpts.Count();
                qpts.Add(new Vector3(-GS, F, z - w));
                qpts.Add(new Vector3(-GS, F, z + w));
                qpts.Add(new Vector3(GS, F, z - w));
                qpts.Add(new Vector3(GS, F, z + w));
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
            }
            Vector3[] nrm = new Vector3[qpts.Count];
            for (int idx = 0; idx < nrm.Length; ++idx) nrm[idx] = new Vector3(0, 0, 1);
            genVertexArray = new VertexArray(this._Program, qpts.ToArray(), ind.ToArray(), colors.ToArray(), nrm);
        }


        public void Render(Matrix4 viewProj)
        {
            if (isDirty)
            {
                Update();
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
