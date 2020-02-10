using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;

namespace Dopple
{
    public class Origin
    {
        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;
        private Program _ProgramCam;
        private Program _ProgramSel;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray vaOrigin;
        private VertexArray vaCamBox;
        private VertexArray []vaSelections;
        bool hasNewFrame = true;

        public Frame currentFrame;
        public List<Visual> Visuals = new List<Visual>();

        public Frame CurrentFrame
        {
            get { return this.currentFrame; }
            set { this.currentFrame = value; hasNewFrame = true; }
        }

        public Origin()
        {
            _Program = Program.FromFiles("Origin.vert", "Origin.frag");
            _ProgramCam = Program.FromFiles("Origin.vert", "OriginCam.frag");
            _ProgramSel = Program.FromFiles("Selection.vert", "Selection.frag");
        }

        int yPlaneSize = 0;
        void LoadOrigin()
        {
            List<Vector3> yPlane = new List<Vector3>();
            List<Vector3> yTx = new List<Vector3>();
            const int sz = 4;
            for (int x = 0; x <= sz; ++x)
            {
                yPlane.Add(new Vector3(-sz / 2, 0, x - sz / 2));
                yPlane.Add(new Vector3(sz / 2, 0, x - sz / 2));
                yPlane.Add(new Vector3(x - sz / 2, 0, -sz / 2));
                yPlane.Add(new Vector3(x - sz / 2, 0, sz / 2));
                yTx.Add(new Vector3(1, 0, 0));
                yTx.Add(new Vector3(1, 0, 0));
                yTx.Add(new Vector3(1, 0, 0));
                yTx.Add(new Vector3(1, 0, 0));

                yPlane.Add(new Vector3(0, -sz / 2, x - sz / 2));
                yPlane.Add(new Vector3(0, sz / 2, x - sz / 2));
                yPlane.Add(new Vector3(0, x - sz / 2, -sz / 2));
                yPlane.Add(new Vector3(0, x - sz / 2, sz / 2));
                yTx.Add(new Vector3(0, 1, 0));
                yTx.Add(new Vector3(0, 1, 0));
                yTx.Add(new Vector3(0, 1, 0));
                yTx.Add(new Vector3(0, 1, 0));

                yPlane.Add(new Vector3(-sz / 2, x - sz / 2, 0));
                yPlane.Add(new Vector3(sz / 2, x - sz / 2, 0));
                yPlane.Add(new Vector3(x - sz / 2, -sz / 2, 0));
                yPlane.Add(new Vector3(x - sz / 2, sz / 2, 0));
                yTx.Add(new Vector3(0, 0, 1));
                yTx.Add(new Vector3(0, 0, 1));
                yTx.Add(new Vector3(0, 0, 1));
                yTx.Add(new Vector3(0, 0, 1));
            }

            for (int i = 0; i < yPlane.Count; ++i)
            {
                yPlane[i] = yPlane[i] * 0.1f;
            }
            yPlaneSize = yPlane.Count;
            vaOrigin = new VertexArray(_Program, yPlane.ToArray(), (uint [])null, yTx.ToArray(), null);
            hasNewFrame = false;
        }

        public void UpdateVisuals()
        {
            vaSelections = null;
            if (this.Visuals.Count == 0)
                return;
            vaSelections = new VertexArray[this.Visuals.Count];
            int idx = 0;
            foreach (Visual v in this.Visuals)
            {
                vaSelections[idx] = new VertexArray(_ProgramSel, v.pos, v.indices, null, v.normal);
                idx++;
            }
        }

        void LoadCamBox()
        {
            if (CurrentFrame != null && CurrentFrame.hdr != null)
            {
                Vector3[] frustum = new Vector3[_Cube.Length];
                Matrix4 camViewProj = Matrix4.CreateScale(2.0f / 3.0f, 1, 1) * CurrentFrame.hdr.projectionMat *
                    CurrentFrame.hdr.viewMat * CurrentFrame.hdr.worldMat;
                Matrix4 invCamViewProj = camViewProj.Inverted();
                Matrix4 matrix = invCamViewProj * Matrix4.CreateScale(2, -2, 1) * Matrix4.CreateTranslation(-0.5f, -0.5f, 0);
                for (int i = 0; i < frustum.Length; ++i)
                {
                    float minz = 0.89f;
                    float maxz = 0.995f;
                    float lerpZ = (_Cube[i].Z * (maxz - minz)) + minz;
                    Vector4 outPt = matrix * new Vector4(_Cube[i].X, _Cube[i].Y, lerpZ, 1);
                    frustum[i] = new Vector3(outPt.X / outPt.W, outPt.Y / outPt.W, outPt.Z / outPt.W);
                }
                vaCamBox = new VertexArray(_ProgramCam, frustum, Origin._CubeIndices, Origin._Cube, null);
            }
            else
                vaCamBox = null;
        }
        public void Render(Matrix4 viewProj)
        {
            if (hasNewFrame)
            {
                LoadOrigin();
                LoadCamBox();
            }
            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);
            // Compute the model-view-projection on CPU
            Matrix4 matWorldViewProj = viewProj;
            // Set uniform state
            GL.UniformMatrix4(_Program.LocationMVP, false, ref matWorldViewProj);
            // Use the vertex array
            GL.BindVertexArray(vaOrigin.ArrayName);
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            GL.DrawArrays(PrimitiveType.Lines, 0, yPlaneSize);

            if (false && vaCamBox != null)
            {
                matWorldViewProj = viewProj;// * faceWorldMat.Inverse;
                GL.UseProgram(_ProgramCam.ProgramName);
                // Set uniform state
                GL.UniformMatrix4(_Program.LocationMVP, false, ref matWorldViewProj);
                // Use the vertex array
                GL.BindVertexArray(vaCamBox.ArrayName);
                GL.DrawElements(PrimitiveType.Triangles, Origin._CubeIndices.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
            }

            if (vaSelections != null)
            {
                GL.Disable(EnableCap.DepthTest);
                matWorldViewProj = viewProj;// * faceWorldMat.Inverse;
                GL.UseProgram(_ProgramSel.ProgramName);
                // Set uniform state
                GL.UniformMatrix4(_ProgramSel.LocationMVP, false, ref matWorldViewProj);
                for (int idx = 0; idx < vaSelections.Length; ++idx)
                {
                    _ProgramSel.Set3("meshColor", this.Visuals[idx].color);
                    if (this.Visuals[idx].wireframe)
                        vaSelections[idx].DrawWireframe();
                    else
                        vaSelections[idx].Draw();
                }
                GL.Enable(EnableCap.DepthTest);
            }
        }
        Vector2 Barrel(Vector2 v)
        {
            // parameters for correction
            double paramA = -0.007715; // affects only the outermost pixels of the image
            double paramB = 0.026731; // most cases only require b optimization
            double paramC = 0.0; // most uniform correction
            double paramD = 1.0 - paramA - paramB - paramC; // describes the linear scaling of the image
            // distance or radius of dst image
            double dstR = v.Length;

            // distance or radius of src image (with formula)
            double srcR = (paramA * dstR * dstR * dstR + paramB * dstR * dstR + paramC * dstR + paramD) * dstR;

            // comparing old and new distance to get factor
            double factor = Math.Abs(dstR / srcR);
            factor = Math.Pow(factor, 4);
            // coordinates in source image

            return new Vector2(v.X * (float)factor, v.Y * (float)factor);
        }



        private readonly string[] _VertexSourceGL = {
            "#version 150 compatibility\n",
            "uniform mat4 uMVP;\n",
            "in vec3 aPosition;\n",
            "in vec3 aTexCoord;\n",
            "out vec3 vTexCoord;\n",
            "void main() {\n",
            "	gl_Position = uMVP * vec4(aPosition, 1.0);\n",
            "	vTexCoord = aTexCoord;\n",
            "}\n"
        };


        string[] _FragmentSourceGL =
        {
              @"#version 150 compatibility
              in vec3 vTexCoord;
              void main()
              {
                gl_FragColor = vec4(vTexCoord.xyz,1);
              }"
        };

        string[] _FragmentSourceGLCam =
        {
              @"#version 150 compatibility
              in vec3 vTexCoord;
              void main()
              {
                gl_FragColor = vec4(vTexCoord, 1) * 0.5;
              }"
        };

        private static readonly Vector3[] _ArrayPosition = new Vector3[] {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
        };

        /// <summary>
        /// Vertex color array.
        /// </summary>
        private static readonly Vector3[] _ArrayTexCoord = new Vector3[] {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
        };

        private static readonly Vector3[] _Cube = new Vector3[] {
            new Vector3(0.0f, 0.0f, 0.0f),  // 0 
            new Vector3(1.0f, 0.0f, 0.0f),  // 1
            new Vector3(1.0f, 1.0f, 0.0f),  // 2
            new Vector3(0.0f, 1.0f, 0.0f),  // 3
            new Vector3(0.0f, 0.0f, 1.0f),  // 4
            new Vector3(1.0f, 0.0f, 1.0f),  // 5
            new Vector3(1.0f, 1.0f, 1.0f),  // 6
            new Vector3(0.0f, 1.0f, 1.0f),  // 7
        };

        private static readonly ushort[] _CubeIndices = new ushort[]
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
            2, 6, 5
        };
        public void Dispose()
        {
            _Program?.Dispose();
            _ProgramCam?.Dispose();
            vaOrigin?.Dispose();
            vaCamBox?.Dispose();
        }
    }
}
