using System;
using System.Windows.Forms;
using OpenGL;
using Khronos;
using System.Runtime.InteropServices;
using Dopple;
using System.Drawing;

namespace TcpServerDemo
{
    // Copyright (C) 2016-2018 Luca Piccioni
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // 
    // The above copyright notice and this permission notice shall be included in all
    // copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    // SOFTWARE.

    /// <summary>
    /// Sample drawing a simple, rotating and colored triangle.
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - OpenGL 3.2
    /// - OpenGL 1.1/1.0 (deprecated)
    /// - OpenGL ES2
    /// </remarks>
    public partial class VideoViewer : UserControl
    {
        /// <summary>
        /// Construct a SampleForm.
        /// </summary>
        public VideoViewer()
        {
            InitializeComponent();
            this.RenderControl.MouseDown += RenderControl_MouseDown;
            this.RenderControl.MouseUp += RenderControl_MouseUp;
            this.RenderControl.MouseMove += RenderControl_MouseMove;
        }


        enum ActiveTool
        {
            None,
            MoveEye,
            MoveLook,
            ObjectMove,
            ObjectScale
        }

        Point mouseDownPt;
        ActiveTool activeTool = ActiveTool.None;
        //Vector2 faceScale = new Vector2(1.2926209f, 1.10739434f);
        //Vector2 faceOffset = new Vector2(-0.007633589f, -0.005281685f);

        Vector2 faceScale = new Vector2(1.0f, 1.0f);
        Vector2 faceOffset = new Vector2(0.0f, 0.0f);

        Vector2 imgScale = new Vector2(0.9389313f, 0.941901267f);
        Vector2 imgOffset = new Vector2(-0.00508905947f, 0.0123239458f);
        Vector2 faceOffsetDn;
        Vector2 faceScaleDn;
        private void RenderControl_MouseMove(object sender, MouseEventArgs e)
        {
            float xMove = (e.X - mouseDownPt.X);
            float yMove = (e.Y - mouseDownPt.Y);
            float xiMove = xMove / (float)this.ClientRectangle.Width;
            float yiMove = yMove / (float)this.ClientRectangle.Height;
            if (this.activeTool == ActiveTool.MoveEye)
            {
                if (e.Button == MouseButtons.Left)
                {

                    Quaternion q1 = this.mouseDownlookAngle * new Quaternion(new Vector3(0, 1, 0), -xMove);
                    this.lookAngle = q1 * new Quaternion(new Vector3(1, 0, 0), -yMove);
                }
                else
                {
                    this.lookDistance = Math.Max(this.mouseDownLookDist + yMove * 0.01f, 0);
                }
            }
            else if (this.activeTool == ActiveTool.ObjectScale)
            {
                faceScale = new Vector2(faceScaleDn.x + xiMove,
                    faceScaleDn.y - yiMove);
            }
            else if (this.activeTool == ActiveTool.ObjectMove)
            {
                faceOffset = new Vector2(faceOffsetDn.x + xiMove,
                    faceOffsetDn.y - yiMove);
            }
            if (this.activeTool != ActiveTool.None)
                this.RenderControl.Invalidate();
        }

        private void RenderControl_MouseUp(object sender, MouseEventArgs e)
        {
            this.activeTool = ActiveTool.None;
            this.RenderControl.Capture = false;
        }

        private void RenderControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.activeTool = e.Button == MouseButtons.Left ? ActiveTool.ObjectMove : ActiveTool.ObjectScale;
            this.mouseDownPt = e.Location;
            this.faceOffsetDn = this.faceOffset;
            this.faceScaleDn = this.faceScale;
            this.mouseDownLookDist = this.lookDistance;
            this.mouseDownlookAngle = this.lookAngle;
            this.RenderControl.Capture = true;
        }

        public event EventHandler OnRendererCreated;
        Quaternion mouseDownlookAngle;
        Quaternion lookAngle = new Quaternion(new Vector3(0, 0, 1), 0);
        float mouseDownLookDist;
        float lookDistance = 1.0f;
        Vector3 lookPos = new Vector3(0, 0, 0);
        Vector3 upVec = new Vector3(0, 1, 0);
        Matrix4 projectionMat;
        Matrix4 faceWorldMat;
        Matrix4 viewMat;

        void CheckGlErrors()
        {
            ErrorCode ec = Gl.GetError();
            if (ec != ErrorCode.NoError)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public enum ViewMode
        {
            eImage = 1,
            eDepth = 2,
            eFaceMesh = 4,
            eFacePtCloud = 8,
            eAll = 15
        }


        ViewMode viewMode = ViewMode.eImage | ViewMode.eFaceMesh;

        public ViewMode VMode { get { return this.viewMode; } set { this.viewMode = value;
                this.RenderControl.Invalidate();
            } }

        /// <summary>
        /// Allocate GL resources or GL states.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="object"/> that has rasied the event.
        /// </param>
        /// <param name="e">
        /// The <see cref="GlControlEventArgs"/> that specifies the event arguments.
        /// </param>
        private void RenderControl_ContextCreated(object sender, GlControlEventArgs e)
        {
            GlControl glControl = (GlControl)sender;
            // GL Debugging
            if (Gl.CurrentExtensions != null && Gl.CurrentExtensions.DebugOutput_ARB)
            {
                //Gl.DebugMessageCallback(GLDebugProc, IntPtr.Zero);
                // Gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, null, true);
            }

            RenderControl_Create();
            // Uses multisampling, if available
            if (Gl.CurrentVersion != null && Gl.CurrentVersion.Api == KhronosVersion.ApiGl && glControl.MultisampleBits > 0)
                Gl.Enable(EnableCap.Multisample);

            OnRendererCreated(this, new EventArgs());
        }

        private void RenderControl_Render(object sender, GlControlEventArgs e)
        {
            // Common GL commands
            Control senderControl = (Control)sender;

            Gl.Viewport(0, 0, senderControl.ClientSize.Width, senderControl.ClientSize.Height);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            DoRender();
        }

        private void RenderControl_ContextUpdate(object sender, GlControlEventArgs e)
        {
        }

        private void RenderControl_ContextDestroying(object sender, GlControlEventArgs e)
        {
        }

        private static void GLDebugProc(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string msg = Marshal.PtrToStringAnsi(message);
            Console.WriteLine($"{source}, {type}, {severity}: {msg}");
        }


        VideoMesh videoMesh = null;
        FaceMesh faceMesh = null;
        FacePtCloud facePtCloud = null;
        Origin origin = null;
        public VideoMesh VideoMesh { get {return this.videoMesh; } }
        public FaceMesh FaceMesh { get { return this.faceMesh; } }
        public FacePtCloud FacePtCloud { get { return this.facePtCloud; } }
        private void RenderControl_Create()
        {
            videoMesh = new VideoMesh();
            faceMesh = new FaceMesh();
            facePtCloud = new FacePtCloud();
            origin = new Origin();
        }
        
        private void DoRender()
        {
            Gl.Clear(ClearBufferMask.DepthBufferBit);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            Vector3 angleVec = this.lookAngle * new Vector3(0, 0, 1);
            angleVec *= this.lookDistance;
            Vector3 eyePos = this.lookPos + angleVec;
            Matrix4 viewProj =
                viewMat * projectionMat;// Matrix4.LookAt(eyePos, this.lookPos, this.upVec);

            if ((viewMode & ViewMode.eImage) != 0)
            {
                Gl.Disable(EnableCap.DepthTest);
                this.videoMesh.imgOffset = new Vertex2f(-imgOffset.y, -imgOffset.x);
                this.videoMesh.imgScale = new Vertex2f(imgScale.y, imgScale.x);
                this.videoMesh.Render();
            }

            Gl.Enable(EnableCap.DepthTest);
            if ((viewMode & ViewMode.eFacePtCloud) != 0)
            {
                this.facePtCloud.Render(faceWorldMat * viewProj);
            }

            if ((viewMode & ViewMode.eFaceMesh) != 0)
            {
                this.faceMesh.faceOffset = new Vertex2f(faceOffset.x, faceOffset.y);
                this.faceMesh.faceScale = new Vertex2f(faceScale.x, faceScale.y);
                this.faceMesh.Render(faceWorldMat * viewProj);
            }

            origin.Render(viewProj);
        }

        public void UpdateRender()
        {
            this.RenderControl.Invalidate();
        }

        public void InitViewPos(Frame f)
        {
            if (f.hdr == null)
                return;
            this.faceWorldMat = new Matrix4(f.hdr.worldMat.ToFloatArray());
            //this.projectionMat = Matrix4.Perspective(60, 2.0f / 3.0f, 0.1f, 10.0f);
            this.projectionMat = new Matrix4(f.hdr.projectionMat.ToFloatArray());
            this.viewMat = new Matrix4(f.hdr.viewMat.ToFloatArray());
            this.lookPos = new Vector3(f.hdr.worldMat.vec[3].x,
                f.hdr.worldMat.vec[3].y,
                f.hdr.worldMat.vec[3].z);
        }
        public void SetCurrentFrame(Frame f)
        {
            this.videoMesh.CurrentFrame = f;
            this.facePtCloud.CurrentFrame = f;
            this.faceMesh.CurrentFrame = f;
            this.origin.CurrentFrame = f;
            this.RenderControl.Invalidate();
            InitViewPos(f);
        }

    }
}
