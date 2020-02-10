using System;
using System.Drawing;
using System.Windows.Forms;
using OpenTK.Graphics.ES30;
using OpenTK;
using Dopple;
using System.Collections.Generic;
using System.Windows.Input;

namespace FaceServer
{
    public partial class GLView : UserControl
    {
        public enum ViewMode
        {
            eImage = 1,
            eDepth = 2,
            eFaceMesh = 4,
            eCombinedFace = 16,
            eAll = 31
        }


        ViewMode viewMode = ViewMode.eCombinedFace;
        int currentFB;
        int selectionFB;
        int pickTexSize = 1024;

        public class OnItemPickedArgs : EventArgs
        {
            public int ItemIdx;
            public int PartIdx;
            public bool IsAdditive;
        }
        public event EventHandler<OnItemPickedArgs> OnItemPicked;
        public ViewMode VMode
        {
            get { return this.viewMode; }
            set
            {
                this.viewMode = value;
                this.glControl.Invalidate();
            }
        }

        void glControl_Resize(object sender, EventArgs e)
        {
            OpenTK.GLControl c = sender as OpenTK.GLControl;

            if (c.ClientSize.Height == 0)
                c.ClientSize = new System.Drawing.Size(c.ClientSize.Width, 1);
            GL.Viewport(0, 0, c.ClientSize.Width, c.ClientSize.Height);
            System.Diagnostics.Debug.WriteLine($"gl {c.ClientSize.Height}");
        }

        /// <summary>
        /// Construct a SampleForm.
        /// </summary>
        public GLView()
        {
            InitializeComponent();
            string[] names = Enum.GetNames(typeof(CameraMode));
            foreach (string name in names)
            {
                this.cameraTypeCB.Items.Add(name);
            }

            this.cameraTypeCB.SelectedIndex = 1;
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            glControl.Resize += new EventHandler(glControl_Resize);
            glControl.Paint += GlControl_Paint;
            glControl.MouseDown += RenderControl_MouseDown;
            glControl.MouseUp += RenderControl_MouseUp;
            glControl.MouseMove += RenderControl_MouseMove;
            glControl.MouseWheel += RenderControl_MouseWheel;

            Text =
                GL.GetString(StringName.Vendor) + " " +
                GL.GetString(StringName.Renderer) + " " +
                GL.GetString(StringName.Version);

            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);

            // Ensure that the viewport and projection matrix are set correctly.
            glControl_Resize(glControl, EventArgs.Empty);
            RenderControl_Create();
            OnRendererCreated(this, new EventArgs());

            this.currentFB = 0;
            CreateSelectionFB();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.currentFB);
            UpdateViewProj();
        }

        void CreateSelectionFB()
        {
            this.selectionFB = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, selectionFB);
            // The texture we're going to render to
            int renderedTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);

            GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba8,
                pickTexSize, pickTexSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            int depthrenderbuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthrenderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent32f, pickTexSize, pickTexSize);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthrenderbuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, renderedTexture, 0);
            DrawBufferMode[] drawBufs = new DrawBufferMode[] { DrawBufferMode.ColorAttachment0 };
            GL.DrawBuffers(1, drawBufs);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new Exception("Framebuffer");
        }

        void UpdateViewProj()
        {
            this.projectionMat = Matrix4.CreatePerspectiveFieldOfView(60 * (float)Math.PI / 180.0f, 2.0f / 3.0f, 0.1f, 10.0f);
            if (this.selectedMesh != null && cameraMode == CameraMode.PivotFace)
            {
                Vector3 meshOrigin = this.selectedMesh.mesh.CalcCenter();
                Vector3 lookvec = lookAngle.Val.Normalized() * lookDist;
                Vector3 eyePos = meshOrigin + lookvec;
                this.viewMat = Matrix4.LookAt(eyePos, meshOrigin, this.upVec);
            }
            else
            {
                Vector3 lookvec = lookAngle.Val.Normalized() * lookDist;
                Vector3 eyePos = lookAt.Val + lookvec;
                this.viewMat = Matrix4.LookAt(eyePos, lookAt.Val, this.upVec);
            }
        }

        private void GlControl_Paint(object sender, PaintEventArgs e)
        {
            DoRender(false);
            glControl.SwapBuffers();

            if (this.xPick >= 0)
            {
                RGBA pixel = new RGBA();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.selectionFB);
                GL.Viewport(new Rectangle(0, 0, pickTexSize, pickTexSize));
                DoRender(true);
                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                GL.ReadPixels<RGBA>(xPick, yPick, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ref pixel);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.currentFB);
                this.xPick = this.yPick = -1;
                GL.Viewport(this.glControl.ClientRectangle);
                PickItem(pixel.B, (pixel.R | (pixel.G << 8)), this.isSelectionAdditive);
            }
        }

        void PickItem(int itemIdx, int partIdx, bool isAdditive)
        {
            OnItemPickedArgs oipa = new OnItemPickedArgs();
            oipa.ItemIdx = itemIdx;
            oipa.PartIdx = partIdx;
            oipa.IsAdditive = isAdditive;
            OnItemPicked(this, oipa);
        }
        enum ActiveTool
        {
            None,
            Select,
            CameraOrbit,
            PtMeshMove,
            PtMeshRotate,
            TwoDRotate,
            ThreeDRotate,
            ViewPortMove,
            ViewPortScale
        }

        enum CameraMode
        {
            PivotOrigin,
            PivotFace,
            VideoAlign
        }

        ActiveMesh selectedMesh;
        public ActiveMesh SelectedMesh
        {
            get { return this.selectedMesh; }
            set
            {
                this.selectedMesh = value; this.glControl.Invalidate();
            }
        }

        public struct DnVal<V>
        {
            V down;
            V val;

            public DnVal(V v)
            { this.down = this.val = v;  }
            public void Push()
            {
                down = val;
            }
            public V Down { get { return this.down; } }
            public V Val { get { return this.val; } set { this.val = value; } }
        }
        
        CameraMode cameraMode;
        Point mouseDownPt;
        MouseButtons buttonsDown = MouseButtons.None;
        ActiveTool activeTool = ActiveTool.Select;
        Vector2 faceScale = new Vector2(1.0f, 1.0f);
        Vector2 faceOffset = new Vector2(0.0f, 0.0f);
        Vector3 facePosition;
        bool lockTransformToFirstFrame = false;

        public event EventHandler OnRendererCreated;
        public event EventHandler OnMeshUpdated;
        DnVal<Vector3> lookAt = new DnVal<Vector3>(Vector3.Zero);
        DnVal<Vector3> lookAngle = new DnVal<Vector3>(Vector3.UnitZ);
        Vector3 xVecRotDn;
        Vector3 yVecRotDn;
        float lookDist = 1.0f;
        float lookDistDn;
        Vector3 upVec = new Vector3(0, 1, 0);
        Vector3 meshTranslationDn;
        Matrix4 cameraProjectionMat;
        Matrix4 faceWorldMat;
        Matrix4 cameraViewMat;
        Matrix4 viewMat;
        Matrix4 projectionMat;
        Quaternion meshRotationDn;
        DnVal<Quaternion> threeDRot = new DnVal<Quaternion>(Quaternion.Identity);
        DnVal<Vector3> threeDTranslate = new DnVal<Vector3>(Vector3.Zero);
        
        struct RGBA
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;
        }

        int xPick = -1;
        int yPick = -1;
        bool isSelectionAdditive;
        void SelectItemFromPt(Point pt, bool isAdditive)
        {
            this.xPick = (int)(pt.X / (float)glControl.ClientRectangle.Width * pickTexSize);
            this.yPick = pickTexSize - (int)(pt.Y / (float)glControl.ClientRectangle.Height * pickTexSize);
            this.isSelectionAdditive = isAdditive;
            this.UpdateRender();
        }

        public void SetInfoText(string text)
        {
            this.alignScoreTB.Text = text;
        }

        public void AddInfoText(string text)
        {
            this.alignScoreTB.Text += text;
        }

        public void KeyStateChanged(Keys keys, bool isdown)
        {
        }
        private void RenderControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left &&
                this.activeTool == ActiveTool.Select)
            {
                bool isAdditive = Control.ModifierKeys == Keys.Shift;
                SelectItemFromPt(e.Location, isAdditive);
            }
            else
            {
                this.buttonsDown |= e.Button;
                this.mouseDownPt = e.Location;
                var invViewProj = (this.viewMat * this.projectionMat).Inverted();
                var vecMid = Vector4.Transform(new Vector4(0, 0, 0.5f, 1), invViewProj);
                var vecX = Vector4.Transform(new Vector4(1, 0, 0.5f, 1), invViewProj);
                var vecY = Vector4.Transform(new Vector4(0, 1, 0.5f, 1), invViewProj);
                vecMid /= vecMid.W;
                vecX /= vecX.W;
                vecY /= vecY.W;
                vecX = vecMid - vecX;
                vecY = vecMid - vecY;
                xVecRotDn = (new Vector3(vecX.X, vecX.Y, vecX.Z)).Normalized();
                yVecRotDn = (new Vector3(vecY.X, vecY.Y, vecY.Z)).Normalized();
                this.upVec = -yVecRotDn;

                this.lookAngle.Push();
                this.lookAt.Push();
                this.lookDistDn = this.lookDist;
                this.threeDRot.Push();
                this.threeDTranslate.Push();
                this.glControl.Capture = true;
                if (this.selectedMesh != null)
                {
                    meshTranslationDn = this.selectedMesh.translation;
                    meshRotationDn = this.selectedMesh.rotation;
                }
            }
        }

        private void RenderControl_MouseWheel(object sender, MouseEventArgs e)
        {
            float wheelScale = 0.0025f * this.lookDist;
            this.lookDist = this.lookDist + e.Delta * wheelScale;
            this.UpdateViewProj();
            this.glControl.Invalidate();
        }

        private void RenderControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.buttonsDown == MouseButtons.None)
                return;

            float xMove = (e.X - mouseDownPt.X);
            float yMove = (e.Y - mouseDownPt.Y);
            float xiMove = xMove / (float)glControl.ClientRectangle.Width;
            float yiMove = yMove / (float)glControl.ClientRectangle.Height;
            float rotScale = 0.01f;
            float moveScale = 0.001f;

            if ((buttonsDown & MouseButtons.Middle) == MouseButtons.Middle)
            {
                if (Control.ModifierKeys == Keys.Shift)
                {
                    this.lookAt.Val = this.lookAt.Down + xVecRotDn * xMove * moveScale +
                        -yVecRotDn * yMove * moveScale;
                }
                else
                {
                    this.lookAngle.Val = Quaternion.FromAxisAngle(yVecRotDn, xMove * rotScale) *
                    Quaternion.FromAxisAngle(xVecRotDn, yMove * rotScale) *
                    this.lookAngle.Down;
                }
                this.UpdateViewProj();
            }
            else
            {
                switch (this.activeTool)
                {
                    case ActiveTool.CameraOrbit:
                        {
                            if (buttonsDown == MouseButtons.Left)
                            {
                            }
                            else
                            {
                            }
                            this.UpdateViewProj();
                            break;
                        }
                    case ActiveTool.PtMeshMove:
                        {
                            if (this.selectedMesh != null)
                            {
                                if (Control.ModifierKeys == Keys.Shift)
                                    this.selectedMesh.translation = this.meshTranslationDn + (new Vector3(0, -yiMove, xiMove) * 0.1f);
                                else
                                    this.selectedMesh.translation = this.meshTranslationDn + (new Vector3(xiMove, -yiMove, 0) * 0.1f);

                                OnMeshUpdated(this, new EventArgs());
                            }
                            break;
                        }
                    case ActiveTool.PtMeshRotate:
                        {
                            if (this.selectedMesh != null)
                            {
                                if (Control.ModifierKeys == Keys.Shift)
                                    this.selectedMesh.rotation = this.meshRotationDn * Quaternion.FromAxisAngle(new Vector3(0, 0, 1), xiMove);
                                else if (Control.ModifierKeys == Keys.Control)
                                    this.selectedMesh.rotation = this.meshRotationDn * Quaternion.FromAxisAngle(new Vector3(1, 0, 0), xiMove);
                                else
                                    this.selectedMesh.rotation = this.meshRotationDn * Quaternion.FromAxisAngle(new Vector3(0, 1, 0), xiMove);
                                OnMeshUpdated(this, new EventArgs());
                            }
                            break;
                        }
                    case ActiveTool.ThreeDRotate:
                        {
                            if (buttonsDown == MouseButtons.Left)
                            {
                                if (Control.ModifierKeys == Keys.Shift)
                                    this.threeDRot.Val = this.threeDRot.Down * Quaternion.FromAxisAngle(new Vector3(0, 0, 1), xiMove);
                                else if (Control.ModifierKeys == Keys.Control)
                                    this.threeDRot.Val = this.threeDRot.Down * Quaternion.FromAxisAngle(new Vector3(1, 0, 0), xiMove);
                                else
                                    this.threeDRot.Val = this.threeDRot.Down * Quaternion.FromAxisAngle(new Vector3(0, 1, 0), xiMove);
                            }
                            else
                            {
                                if (Control.ModifierKeys == Keys.Shift)
                                    this.threeDTranslate.Val = this.threeDTranslate.Down + (new Vector3(0, -yiMove, xiMove) * 0.1f);
                                else
                                    this.threeDTranslate.Val = this.threeDTranslate.Down + (new Vector3(xiMove, -yiMove, 0) * 0.1f);
                            }

                            UpdateScore(true);
                            break;
                        }
                }
            }

            this.glControl.Invalidate();
        }

        private void RenderControl_MouseUp(object sender, MouseEventArgs e)
        {
            this.buttonsDown = this.buttonsDown & (~e.Button);
            glControl.Capture = false;
        }

        void CheckGlErrors()
        {
            ErrorCode ec = GL.GetError();
            if (ec != ErrorCode.NoError)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        VideoMesh videoMesh = null;
        CombinedFace combinedFace = null;
        AlignmentVis alignmentVis = null;
        public ThreeDPointVis threedpointvis = null;
        Origin origin = null;
        public VideoMesh VideoMesh { get { return this.videoMesh; } }
        public bool ApplyAlignTransform { get { return alignmentVis.ApplyAlignTransform; } set { alignmentVis.ApplyAlignTransform = value; } }
        private void RenderControl_Create()
        {
            videoMesh = new VideoMesh();
            combinedFace = new CombinedFace();
            alignmentVis = new AlignmentVis();
            threedpointvis = new ThreeDPointVis();
            origin = new Origin();
        }

        private void DoRender(bool selectMode)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.FrontAndBack);
            Matrix4 viewProj;
            if (this.cameraMode == CameraMode.VideoAlign)
            {
                viewProj = this.cameraViewMat * this.cameraProjectionMat;
            }
            else
            {
                viewProj = viewMat * projectionMat;
            }

            if ((viewMode & ViewMode.eImage) != 0 ||
                (viewMode &ViewMode.eDepth) != 0)
            {
                GL.Disable(EnableCap.DepthTest);
                float num = 0;
                float den = 1;
                if ((viewMode & ViewMode.eImage) != 0)
                    den += 1.0f;
                if ((viewMode & ViewMode.eDepth) != 0)
                    num += 1.0f;

                this.videoMesh.Render(num / den);
            }

            GL.Enable(EnableCap.DepthTest);

            if ((viewMode & ViewMode.eCombinedFace) != 0)
            {
                this.combinedFace.Render(viewProj, selectMode);
                this.alignmentVis.Render(viewProj);
            }

            Matrix4 worldMat = Matrix4.CreateFromQuaternion(threeDRot.Val) *
                Matrix4.CreateTranslation(threeDTranslate.Val);

            this.threedpointvis.Render(worldMat, viewProj);
            if (!selectMode)
                origin.Render(viewProj);

        }

        public void AddVisuals(List<Visual> v)
        {
            this.origin.Visuals.Clear();
            this.origin.Visuals.AddRange(v);
            this.origin.UpdateVisuals();
            this.UpdateRender();
        }

        public void UpdateRender()
        {
            this.glControl.Invalidate();
        }

        public void InitViewPos(Frame f)
        {
            if (f.hdr == null)
                return;
            this.faceWorldMat = f.hdr.worldMat;
            this.cameraProjectionMat = f.hdr.projectionMat;
            this.cameraViewMat = f.hdr.viewMat;
            this.facePosition = new Vector3(f.hdr.worldMat.Row3.X,
                f.hdr.worldMat.Row3.Y,
                f.hdr.worldMat.Row3.Z);
        }
        public void SetCurrentFrame(Frame f)
        {
            this.videoMesh.CurrentFrame = f;
            this.origin.CurrentFrame = f;
            this.glControl.Invalidate();
            if (!lockTransformToFirstFrame)
                InitViewPos(f);
        }

        public void SetCurrentMesh(List<ActiveMesh> m)
        {
            this.combinedFace.CurrentMeshes = m;
        }

        private void CameraTypeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.cameraMode = (CameraMode)this.cameraTypeCB.SelectedIndex;
            UpdateRender();
        }

        void UpdateScore(bool updateBestFit)
        {
            Vector3 axis;
            float r;
            this.threeDRot.Val.ToAxisAngle(out axis, out r);
            axis.Normalize();
            Vector4 axisAng = new Vector4(axis, r);
            if (updateBestFit)
                this.threedpointvis.SetBestFit(axisAng, this.threeDTranslate.Val);
        }
        private void AlignStepBtn_Click(object sender, EventArgs e)
        {
            this.threeDRot.Val = Quaternion.FromAxisAngle(new Vector3(threedpointvis.bestFitRot),
                threedpointvis.bestFitRot.W);
            this.threeDTranslate.Val = threedpointvis.bestFitTrans;
            UpdateScore(false);
            this.glControl.Invalidate();
        }

        private void SelectToolBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (this.selectToolBtn.Checked)
                this.activeTool = ActiveTool.Select;
            else if (this.translateBtn.Checked)
                this.activeTool = ActiveTool.PtMeshMove;
            else if (this.rotateBtn.Checked)
                this.activeTool = ActiveTool.PtMeshRotate;
        }
    }
}
