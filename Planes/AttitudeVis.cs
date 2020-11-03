using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlTypes;
using OpenTK.Graphics.OpenGL;
using Dopple;
using System.Drawing.Drawing2D;

namespace Planes
{
    class AttitudeVis
    {
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>yepf
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray vertexArray = null;
        private Matrix4 videoMatrix;
        bool isDirty = true;

        public AttitudeVis()
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

        public void LoadVideoFrame()
        {
            const int delta = 10;
            double timespan = 2.0f;
            float yScale = 0.5f;

            Vector3 center = new Vector3(0, 1, -5);
            List<MotionPoint> motionPoints = new List<MotionPoint>();

            MotionPoint[] mpts = App.Recording.CurrentFrame.motionPoints;
            var mp = mpts[0];
            Quaternion q = new Quaternion((float)mp.qX, (float)mp.qY, (float)mp.qZ, (float)mp.qW);

            Vector3 grav = new Vector3((float)mp.gX, (float)mp.gY, (float)mp.gZ);
            grav.Normalize();

            Matrix4 rotMat= Matrix4.CreateFromQuaternion(q);

            List<Vector3> qpts = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<uint> ind = new List<uint>();

            Vector3 dx = Vector3.TransformVector(Vector3.UnitX, rotMat);
            Vector3 dy = Vector3.TransformVector(Vector3.UnitY, rotMat);
            Vector3 dz = Vector3.TransformVector(Vector3.UnitZ, rotMat);

            DrawLine(center, center + dx, 0.01f, Vector3.UnitX, qpts, ind, colors);
            DrawLine(center, center + dy, 0.01f, Vector3.UnitY, qpts, ind, colors);
            DrawLine(center, center + dz, 0.01f, Vector3.UnitZ, qpts, ind, colors);
            DrawLine(center, center + grav, 0.05f, Vector3.One, qpts, ind, colors);

            Vector3[] nrm = new Vector3[qpts.Count];
            for (int idx = 0; idx < nrm.Length; ++idx) nrm[idx] = new Vector3(0, 0, 1);
            vertexArray = new VertexArray(this._Program, qpts.ToArray(), ind.ToArray(), colors.ToArray(), nrm);
        }

        void DrawLine(Vector3 pt0, Vector3 pt1, float width, Vector3 color, List<Vector3> pts, List<uint> ind,
            List<Vector3> colors
            )
        {
            uint startIdx = (uint)pts.Count;
            Vector3 dir = (pt1 - pt0).Normalized();
            Vector3 nrm = Vector3.Cross(dir, Vector3.UnitZ);
            pts.Add(pt0 - nrm * width);
            pts.Add(pt0 + nrm * width);
            pts.Add(pt1 - nrm * width);
            pts.Add(pt1 + nrm * width);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            ind.Add(startIdx);
            ind.Add(startIdx + 1);
            ind.Add(startIdx + 2);
            ind.Add(startIdx + 1);
            ind.Add(startIdx + 3);
            ind.Add(startIdx + 2);
        }


        public void Render(Matrix4 viewProj)
        {
            if (isDirty)
            {
                LoadVideoFrame();
                isDirty = false;
            }
            if (vertexArray == null)
                return;
            _Program.Use(0);

            Vector3 scl = new Vector3(1, 0.5f, 1);

            Matrix4 mvp = Matrix4.Identity;

            _Program.SetMat4("uMVP", ref viewProj);
            _Program.Set1("opacity", 1.0f);
            _Program.Set3("meshColor", new Vector3(1, 1, 1));
            _Program.Set1("ambient", 1.0f);
            _Program.Set3("lightPos", new Vector3(2, 5, 2));
            _Program.Set1("opacity", 1.0f);
            Matrix4 matWorldInvT = Matrix4.Identity;
            _Program.SetMat4("uWorld", ref matWorldInvT);
            _Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);
            _Program.SetMat4("uCamMat", ref videoMatrix);
            vertexArray.Draw();
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
