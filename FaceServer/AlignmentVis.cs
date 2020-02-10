using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;

namespace Dopple
{
    public class AlignmentVis
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

        public AlignmentVis()
        {
            _Program = Program.FromFiles("AlignmentVis.vert", "AlignmentVis.frag");
        }
        public bool ApplyAlignTransform { get; set; } = true;

        int[] indexCnt = new int[2];
        void LoadAlignment()
        {
            /*
            if (currentAlign == null)
            {
                vaMeshes = null;
                return;
            }
            bool updateAll = hasNewMesh;
            if (vaMeshes == null)
            {
                vaMeshes = new VertexArray[2];
                updateAll = true;
            }
            
            for (int idx = 0; idx < 2; ++idx)
            {
                Visual v = new Visual();
                currentAlign.GetCubeVisual(out v);
                VertexArray vaMesh = new VertexArray(_Program,
                    v.pos,
                    v.indices,
                    v.color,
                    v.normal);
                vaMeshes[idx] = vaMesh;
                indexCnt[idx] = (int)v.indices.Length;
            }*/
            hasNewMesh = false;
        }
        public void Render(Matrix4 viewProjMat)
        {
            LoadAlignment();
            if (vaMeshes == null)
                return;

            // Select the program for drawing
            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);
            // Compute the model-view-projection on CPU

            for (int idx = 0; idx < 1; ++idx)
            {
                Matrix4 worldMat = Matrix4.Identity;
                Matrix4 matWorldViewProj = worldMat * viewProjMat;
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
