using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;

namespace Dopple
{
    public class CombinedFace
    {
        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray []vaMeshes = null;
        private Visual[] visuals = null;
        private int []visMeshId = null;
        List<ActiveMesh> currentMeshes;
        bool hasNewMesh = false;
        public List<ActiveMesh> CurrentMeshes
        {
            get { return this.currentMeshes; }
            set { this.currentMeshes = value; hasNewMesh = true; }
        }

        public CombinedFace()
        {
            _Program = Program.FromFiles("CombinedFace.vert", "CombinedFace.frag");
        }

        int nPoints = 0;
        void LoadPtCloud()
        {
            if (currentMeshes == null)
                return;
            bool updateAll = hasNewMesh;
            int visCnt = 0;
            foreach (ActiveMesh am in currentMeshes)
            {
                if (am.visuals != null && am.visible)
                {
                    visCnt += am.visuals.Length;
                }
            }

            if (vaMeshes == null ||
                vaMeshes.Length != visCnt)
            {
                vaMeshes = new VertexArray[visCnt];
                visuals = new Visual[visCnt];
                visMeshId = new int[visCnt];
                updateAll = true;
            }
            int idx = 0;
            int meshIdx = 0;
            foreach (ActiveMesh am in currentMeshes)
            {
                if (am.visuals != null && am.visible)
                {
                    foreach (Visual v in am.visuals)
                    {
                        if (updateAll | am.isdirty)
                        {

                            nPoints = v.pos.Length;
                            VertexArray vaMesh = new VertexArray(
                                _Program,
                                v.pos,
                                v.indices,
                                v.texcoord,
                                v.normal);
                            vaMeshes[idx] = vaMesh;
                            visuals[idx] = v;
                            visMeshId[idx] = meshIdx;
                            am.isdirty = false;

                        }
                        idx++;
                    }
                }
                meshIdx++;
            }
            hasNewMesh = false;
        }

        public void Render(Matrix4 viewProjMat, bool selectMode)
        {
            LoadPtCloud();

            if (vaMeshes == null)
                return;
            int idx = 0;
            foreach (VertexArray vaMesh in this.vaMeshes)
            {
                GL.UseProgram(vaMesh.Program.ProgramName);
                ActiveMesh curMesh = this.currentMeshes[visMeshId[idx]];
                if (vaMesh != null)
                {
                    Matrix4 matWorldViewProj =
                        curMesh.WorldTransform * viewProjMat;
                    GL.UniformMatrix4(vaMesh.Program.LocationMVP, false, ref matWorldViewProj);
                    
                    vaMesh.Program.Set3("meshColor", visuals[idx].color);
                    int colorMode = 0;
                    if (!selectMode)
                    { colorMode = (int)visuals[idx].shadingType + 1;  } 
                    vaMesh.Program.Set1("colorMode", colorMode);
                    vaMesh.Program.Set1("opacity", visuals[idx].opacity);
                    if (visuals[idx].wireframe)
                        vaMesh.DrawWireframe();
                    else vaMesh.Draw();
                }
                idx++;
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
