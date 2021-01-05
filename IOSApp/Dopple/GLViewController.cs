using System;
using System.Runtime.InteropServices;
using Foundation;
using UIKit;
using CoreVideo;
using System.Collections.Generic;
using GLObjects;
using System.Linq;
using CoreGraphics;
using OpenGLES;
using GLKit;
using OpenTK.Graphics.ES20;
using ARKit;
using OpenTK;
using CoreMotion;

namespace Dopple
{
#if NATIVELIB
    public class Lib
    {
        [DllImport("__Internal")]
        public static extern void SetPlaneConstants(float minDist, float splitThreshold, float minDPVal);
    }
#endif
    public partial class GLViewController : GLKViewController
    {
#region Constructors
        protected GLViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }
#endregion

        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;
        private Program _FaceProgram;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray vaScreenQuad;
        private VertexArray vaFaceMesh;
        private int nFaceIndices = 0;

        private int _ySamplerLoc;
        private int _uvSamplerLoc;
        private int _depthSamplerLoc;
        private int _imageDepthMixLoc;
        private int _depthVals;
        private int _hasDepthLoc;
        private int _imgScaleLoc;
        private int _imgOffsetLoc;

        public OpenTK.Vector2 imgScale = new OpenTK.Vector2(0.9407393f, 0.943432f);
        public OpenTK.Vector2 imgOffset = new OpenTK.Vector2(-0.0007440706f, 0.008856665f);        
        public float imageDepthMix = 0.5f;
        public OpenTK.Vector2 depthVals = new OpenTK.Vector2(0.2f, 10.0f);
        bool hasDepth = true;
        private OpenTK.Matrix4 faceWorldViewProj;
        double faceTimeStamp = 0;
        double frameTimeStamp = 0;
        CMMotionManager cmMotionManager = new CMMotionManager();

        ARSession arSession;

        private EAGLContext context;
        private CVOpenGLESTexture lumaTexture, chromaTexture;
        private CVOpenGLESTexture depthTexture;
        public CVOpenGLESTextureCache VideoTextureCache { get; private set; }
        DataTransmit dt = new DataTransmit();

        float[] depthFloatBuffer = null;
        int depthWidth;
        int depthHeight;

        public override UIStatusBarStyle PreferredStatusBarStyle()
        {
            return UIStatusBarStyle.LightContent;
        }

        struct Accel3DPoint
        {
            public double X, Y, Z;
            public double timeStamp;

            public Accel3DPoint(double x, double y, double z, double t)
            {
                X = x;
                Y = y;
                Z = z;
                timeStamp = t;
            }

            public override string ToString() => $"X = {X}, Y = {Y}, Z = {Z}";
        }
                
        //List<Accel3DPoint> accelerometerSamples = new List<Accel3DPoint>();
        //List<Accel3DPoint> gyroSamples = new List<Accel3DPoint>();
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            arSession = new ARSession();
            this.vidSclBtn.SetTitle($"Vid {downSampleAmt}x",
                UIControlState.Normal);

            cmMotionManager.DeviceMotionUpdateInterval = 0.01;
            NSOperationQueue dmqueue;
            dmqueue = new NSOperationQueue();
            cmMotionManager.StartDeviceMotionUpdates(dmqueue, (data, error) => {
                var point = new MotionPoint(data.UserAcceleration.X, data.UserAcceleration.Y, data.UserAcceleration.Z,
                    data.RotationRate.x, data.RotationRate.y, data.RotationRate.z,
                    data.Gravity.X, data.Gravity.Y, data.Gravity.Z,
                    data.Attitude.Quaternion.x,
                    data.Attitude.Quaternion.y,
                    data.Attitude.Quaternion.z,
                    data.Attitude.Quaternion.w,
                    data.Timestamp);
                dt.AddMotionPoint(point);
            });
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            context = new EAGLContext(EAGLRenderingAPI.OpenGLES2);
            var glkView = View as GLKView;
            glkView.Context = context;
            glkView.MultipleTouchEnabled = true;

            PreferredFramesPerSecond = 60;
            View.ContentScaleFactor = UIScreen.MainScreen.Scale;
            SetupGL();            
            ARWorldTrackingConfiguration config = new ARWorldTrackingConfiguration();
            config.LightEstimationEnabled = true;
            config.EnvironmentTexturing = AREnvironmentTexturing.Automatic;
            arSession.Delegate = new ARDelegate(this);
            arSession.Run(config);
        }


#region setup 

        private void SetupGL()
        {
            EAGLContext.SetCurrentContext(this.context);
            _Program = new Program("Shaders/VidShader.vert", "Shaders/VidShader.frag");
            ErrorCode cd = GL.GetErrorCode();
            _ySamplerLoc = GL.GetUniformLocation(_Program.ProgramName, "ySampler");
            _uvSamplerLoc = GL.GetUniformLocation(_Program.ProgramName, "uvSampler");
            cd = GL.GetErrorCode(); _depthSamplerLoc = GL.GetUniformLocation(_Program.ProgramName, "depthSampler");
            _imageDepthMixLoc = GL.GetUniformLocation(_Program.ProgramName, "imageDepthMix");
            _depthVals = GL.GetUniformLocation(_Program.ProgramName, "depthVals");
            _hasDepthLoc = GL.GetUniformLocation(_Program.ProgramName, "hasDepth");
            _imgScaleLoc = GL.GetUniformLocation(_Program.ProgramName, "imgScale");
            _imgOffsetLoc = GL.GetUniformLocation(_Program.ProgramName, "imgOffset");
            cd = GL.GetErrorCode();
            vaScreenQuad = new VertexArray(_ArrayPosition, _ArrayElems, _ArrayTexCoord, null);
            cd = GL.GetErrorCode();

            _FaceProgram = new Program("Shaders/FaceMesh.vert", "Shaders/FaceMesh.frag");

            if ((this.VideoTextureCache = CVOpenGLESTextureCache.FromEAGLContext(this.context)) == null)
            {
                Console.WriteLine("Could not create the CoreVideo TextureCache");
                return;
            }
        }

        private void CleanupTextures()
        {
            if (this.lumaTexture != null)
            {
                this.lumaTexture.Dispose();
                this.lumaTexture = null;
            }

            if (this.chromaTexture != null)
            {
                this.chromaTexture.Dispose();
                this.chromaTexture = null;
            }

            if (this.depthTexture != null)
            {
                this.depthTexture.Dispose();
                this.depthTexture = null;
            }

            {
                this.VideoTextureCache.Flush(CVOptionFlags.None);
            }


        }

#endregion

        public override void Update()
        {
        }

        public override void DrawInRect(GLKView view, CGRect rect)
        {
            if (!hasDepth)
                return;

            EAGLContext.SetCurrentContext(this.context);
            GL.ClearColor(0, 0, 1, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);

            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);
            vaScreenQuad.Bind(_Program);

            GL.Uniform1(this._depthSamplerLoc, (int)2);
            GL.Uniform1(this._ySamplerLoc, (int)0);
            GL.Uniform1(this._uvSamplerLoc, (int)1);
            GL.Uniform1(this._imageDepthMixLoc, imageDepthMix);
            GL.Uniform2(this._depthVals, depthVals);
            GL.Uniform2(this._imgScaleLoc, imgScale);
            GL.Uniform2(this._imgOffsetLoc, imgOffset);
            GL.Uniform1(this._hasDepthLoc, hasDepth ? 1 : 0);

            // Compute the model-view-projection on CPU
            Matrix4 mvProj =
                Matrix4.Mult(
                Matrix4.Mult(
                Matrix4.Mult(Matrix4.Scale(2, 2, 1), Matrix4.CreateTranslation(-1, -1, 0.5f)),
                Matrix4.CreateRotationZ((float)(Math.PI * -0.5f))),
                Matrix4.Scale(-1, 1, 1));
           // Set uniform state
            GL.UniformMatrix4(_Program.LocationMVP, false, ref mvProj);
            // Use the vertex array
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, IntPtr.Zero);

            if (this.vaFaceMesh != null && this.faceTimeStamp == this.frameTimeStamp)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.UseProgram(_FaceProgram.ProgramName);
                vaFaceMesh.Bind(_FaceProgram);

                GL.UniformMatrix4(_FaceProgram.LocationMVP, false, ref faceWorldViewProj);
                GL.DrawElements(BeginMode.Lines, nFaceIndices, DrawElementsType.UnsignedShort, IntPtr.Zero);
            }
        }

#region touches
        
        partial void OnRecordBtnDown(UIButton sender)
        {
            dt.IsRecording = !dt.IsRecording;
            this.recordBtn.SetTitle(dt.IsRecording ? "Stop" : "Record",
                 UIControlState.Normal);
            this.recordBtn.BackgroundColor = dt.IsRecording ?
                UIColor.Red : UIColor.White;
            this.recordBtn.SetNeedsDisplay();
        }

        partial void OnLiveBtnDown(UIButton sender)
        {
            dt.LiveTransmit = !dt.LiveTransmit;
            this.liveBtn.BackgroundColor = dt.LiveTransmit ?
                UIColor.Red : UIColor.White;
        }
        
        int downSampleAmt = 4;
        partial void OnVidDownSmpDown(UIKit.UIButton sender)
        {
            downSampleAmt *= 2;
            if (downSampleAmt == 16)
                downSampleAmt = 1;
            this.vidSclBtn.SetTitle($"Vid {downSampleAmt}x",
                UIControlState.Normal);
        }
        class ActiveTouch
        {
            public CGPoint start;
            public CGPoint current;
        }

        int maxTouchesInSession = 0;
        Dictionary<UITouch, ActiveTouch> activeTouches = new Dictionary<UITouch, ActiveTouch>();
        float imgMixDown = 0;
        OpenTK.Vector2 imgScaleDn;
        OpenTK.Vector2 imgOffsetDn;
        int xOrYLocked = 0;

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            if (maxTouchesInSession == 0)
            {
                imgMixDown = imageDepthMix;
                imgScaleDn = imgScale;
                imgOffsetDn = imgOffset;
                xOrYLocked = 0;
            }
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                ActiveTouch at = new ActiveTouch();
                at.start = touch.LocationInView(touch.View);
                activeTouches.Add(touch, at);
                float fltX = (float)at.start.X / (float)this.View.Frame.Width;
                float fltY = (float)at.start.Y / (float)this.View.Frame.Height;

                if (depthFloatBuffer == null)
                    return;
                int iX = (int)(fltX * depthHeight);
                int iY = (int)(fltY * depthWidth);
                double distMeters = this.depthFloatBuffer[iX * depthWidth + iY];
                double feetd = distMeters / 0.3048;
                double feet = Math.Truncate(feetd);
                double inches = Math.Truncate((feetd - feet) * 12.0);
                this.depthLabel.Text = $"{(int)feet}ft {(int)inches}inch";
            }

            maxTouchesInSession = Math.Max(maxTouchesInSession, activeTouches.Count);
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                ActiveTouch at = activeTouches[touch];
                CGPoint pt = touch.LocationInView(touch.View);
                at.current = pt;
            }

            if (true)
            {
                if (maxTouchesInSession == 1 && activeTouches.Count == 1)
                {
                    ActiveTouch at = activeTouches.First().Value;
                    OpenTK.Vector2 offset = new OpenTK.Vector2(                    
                        ((float)at.current.Y - (float)at.start.Y) / (float)this.View.Frame.Height,
                        -((float)at.current.X - (float)at.start.X) / (float)this.View.Frame.Width);
                    imgOffset = imgOffsetDn + offset;
                }

                if (maxTouchesInSession == 2 && activeTouches.Count == 2)
                {
                    var touchArray = activeTouches.Values.ToArray();

                    float distX0 = Math.Abs((float)touchArray[0].start.X - (float)touchArray[1].start.X) / (float)this.View.Frame.Width;
                    float distX1 = Math.Abs((float)touchArray[0].current.X - (float)touchArray[1].current.X) / (float)this.View.Frame.Width;

                    float distY0 = Math.Abs((float)touchArray[0].start.Y - (float)touchArray[1].start.Y) / (float)this.View.Frame.Height;
                    float distY1 = Math.Abs((float)touchArray[0].current.Y - (float)touchArray[1].current.Y) / (float)this.View.Frame.Height;

                    float scaleX = 1;
                    float scaleY = 1;

                    if (xOrYLocked == 0 &&
                        (Math.Abs(distX0 - distX1) > 0.1f ||
                        Math.Abs(distY0 - distY1) > 0.1f))
                    {
                        xOrYLocked = Math.Abs(distX0 - distX1) > Math.Abs(distY0 - distY1) ? 1 : 2;
                    }
                    if (xOrYLocked == 1)
                        scaleY = distY0 / distY1;
                    else if (xOrYLocked == 2)
                        scaleX = distX0 / distX1;

                    //Console.WriteLine($"{scaleX}, {scaleY}");
                    imgScale = new OpenTK.Vector2(imgScaleDn.X * scaleY,
                        imgScaleDn.Y * scaleX);
                }
            }
            if (maxTouchesInSession == 3 && activeTouches.Count == 3)
            {
                ActiveTouch at = activeTouches.First().Value;
                float distX = (float)at.current.X - (float)at.start.X;
                imageDepthMix = Math.Clamp(imgMixDown + (distX / 100.0f), 0, 1);
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                activeTouches.Remove(touch);
            }

            if (activeTouches.Count == 0)
                maxTouchesInSession = 0;

            base.TouchesEnded(touches, evt);
        }


#endregion

        private void TeardownAVCapture() { }

        private void TeardownGL() { }

        public void DidUpdateAnchors(ARSession session, ARAnchor[] anchors)
        {
            foreach (ARAnchor anchor in anchors)
            {
                if (anchor is ARFaceAnchor)
                {
                    ARFaceAnchor faceAnchor = (ARFaceAnchor)anchor;
                    ARFaceGeometry faceGmt = faceAnchor.Geometry;

                    var arCamera = session.CurrentFrame.Camera;
                    Matrix4 viewMatrix = (Matrix4)arCamera.GetViewMatrix(UIInterfaceOrientation.Portrait);
                    viewMatrix.Transpose();
                    Matrix4 projMatrix = (Matrix4)arCamera.GetProjectionMatrix(UIInterfaceOrientation.Portrait,
                        new CGSize(480, 640), 0.1f, 10.0f);
                    projMatrix.Transpose();
                    Matrix4 worldMatrix = (Matrix4)faceAnchor.Transform;
                    worldMatrix.Transpose();
                    {
                        Vector3[] vertices =
                            Array.ConvertAll(faceGmt.GetVertices(), v => new Vector3(v.X, v.Y, v.Z));
                        List<ushort> lineIndicesList = new List<ushort>();
                        short []triindices = faceGmt.GetTriangleIndices();
                        for (int triIdx = 0; triIdx < triindices.Length; triIdx += 3)
                        {
                            ushort t1 = (ushort)triindices[triIdx];
                            ushort t2 = (ushort)triindices[triIdx + 1];
                            ushort t3 = (ushort)triindices[triIdx + 2];
                            lineIndicesList.Add(t1);
                            lineIndicesList.Add(t2);
                            lineIndicesList.Add(t2);
                            lineIndicesList.Add(t3);
                            lineIndicesList.Add(t3);
                            lineIndicesList.Add(t1);
                        }
                        ushort[] indices = lineIndicesList.ToArray();
                        nFaceIndices = indices.Length;
                        Vector3[] texCoords = Array.ConvertAll(faceGmt.GetTextureCoordinates(), tc => new Vector3(tc.X, tc.Y, 0));
                        this.vaFaceMesh = new VertexArray(vertices, indices, texCoords, null);
                        Matrix4 mat = worldMatrix * viewMatrix * projMatrix;                        
                        this.faceWorldViewProj = mat;
                    }



                    double timeStamp = session.CurrentFrame.Timestamp;
                    this.faceTimeStamp = timeStamp;
                    ARFrmHeader arHeader = new ARFrmHeader();
                    arHeader.viewMat = viewMatrix;
                    arHeader.projectionMat = projMatrix;
                    arHeader.worldMat = worldMatrix;
                    {
                        short []triangleIncides = faceGmt.GetTriangleIndices();
                        arHeader.faceIndices = new ushort[triangleIncides.Length];
                        for (int idx = 0; idx < arHeader.faceIndices.Length; ++idx)
                        {
                            arHeader.faceIndices[idx] = (ushort)triangleIncides[idx];
                        }
                    }

                    {
                        OpenTK.NVector3 []vertices = faceGmt.GetVertices();
                        arHeader.faceVertices = new OpenTK.Vector3[vertices.Length];
                        for (int idx = 0; idx < arHeader.faceVertices.Length; ++idx)
                        {
                            arHeader.faceVertices[idx].X = vertices[idx].X;
                            arHeader.faceVertices[idx].Y = vertices[idx].Y;
                            arHeader.faceVertices[idx].Z = vertices[idx].Z;
                        }
                    }
                    {
                        OpenTK.Vector2 []texCoords =faceGmt.GetTextureCoordinates();

                        arHeader.faceTextureCoords = new OpenTK.Vector2[texCoords.Length];
                        for (int idx = 0; idx < arHeader.faceTextureCoords.Length; ++idx)
                        {
                            arHeader.faceTextureCoords[idx].X = texCoords[idx].X;
                            arHeader.faceTextureCoords[idx].Y = texCoords[idx].Y;
                        }
                    }
                    dt.AddARFrmHeader(timeStamp, arHeader);
                }
            }
        }

        public void CameraDidChangeTrackingState(ARSession session, ARCamera camera)
        {

        }

        public void DidUpdateFrame(ARSession session, ARFrame frame)
        {
            this.CleanupTextures();

            // Y-plane
            GL.ActiveTexture(TextureUnit.Texture0);
            All re = (All)0x1903; // GL_RED_EXT, RED component from ARB OpenGL extension

            var imagePixelBuffer = frame.CapturedImage;
            var formatDesc = CVPixelFormatDescription.Create(imagePixelBuffer.PixelFormatType);
            System.Diagnostics.Trace.WriteLine(formatDesc.Description);
            int width = (int)imagePixelBuffer.Width;
            int height = (int)imagePixelBuffer.Height;


            this.lumaTexture = this.VideoTextureCache.TextureFromImage(imagePixelBuffer, true, re, width, height, re, DataType.UnsignedByte, 0, out CVReturn status);
            if (this.lumaTexture == null)
            {
                Console.WriteLine("Error creating luma texture: {0}", status);
                return;
            }

            GL.BindTexture(this.lumaTexture.Target, this.lumaTexture.Name);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

            // UV Plane
            GL.ActiveTexture(TextureUnit.Texture1);
            re = (All)0x8227; // GL_RG_EXT, RED GREEN component from ARB OpenGL extension
            this.chromaTexture = VideoTextureCache.TextureFromImage(imagePixelBuffer, true, re, width / 2, height / 2, re, DataType.UnsignedByte, 1, out status);
            if (this.chromaTexture == null)
            {
                Console.WriteLine("Error creating chroma texture: {0}", status);
                return;
            }

            GL.BindTexture(this.chromaTexture.Target, this.chromaTexture.Name);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);


            GL.ActiveTexture(TextureUnit.Texture2);
            re = (All)0x1903; // GL_RG_EXT, RED GREEN component from ARB OpenGL extension


            bool sendImageData = true;
            double timeStamp = frame.Timestamp;
            this.frameTimeStamp = frame.Timestamp;
            double depthTimeStamp = frame.CapturedDepthDataTimestamp;

            hasDepth = false;

            if (frame.SceneDepth != null)
            {

            }

            if (frame.CapturedDepthData != null)
            {
                var depthData = frame.CapturedDepthData;
                var depthPixelBuffer = depthData.DepthDataMap;
                this.depthTexture = this.VideoTextureCache.TextureFromImage(depthPixelBuffer, true, re, (int)depthPixelBuffer.Width, (int)depthPixelBuffer.Height,
                    re, DataType.Float, 0, out status);

                if (depthFloatBuffer == null)
                {
                    this.depthFloatBuffer = new float[(int)depthPixelBuffer.Width *
                        (int)depthPixelBuffer.Height];
                    this.depthWidth = (int)depthPixelBuffer.Width;
                    this.depthHeight = (int)depthPixelBuffer.Height;
                }

                OpenTK.NMatrix3 calibrationMat =
                    depthData.CameraCalibrationData.IntrinsicMatrix;
                CoreGraphics.CGSize calDims =
                    depthData.CameraCalibrationData.IntrinsicMatrixReferenceDimensions;

                VideoFrame vf = new VideoFrame();
                vf.imageWidth = sendImageData ? (int)imagePixelBuffer.Width : 0;
                vf.imageHeight = sendImageData ? (int)imagePixelBuffer.Height : 0;
                vf.imageBytesPerRow =
                    sendImageData ? (int)imagePixelBuffer.BytesPerRow : 0;

                var arCamera = frame.Camera;
                var viewMatrix = (Matrix4)arCamera.GetViewMatrix(UIInterfaceOrientation.Portrait);
                vf.viewMat = viewMatrix;
                vf.projectionMat = (Matrix4)arCamera.ProjectionMatrix;
                vf.cameraCalibrationVals = new Vector4(
                                calibrationMat.R0C0,
                                calibrationMat.R1C1,
                                calibrationMat.R0C2,
                                calibrationMat.R1C2);
                vf.cameraCalibrationDims.X = (float)calDims.Width;
                vf.cameraCalibrationDims.Y = (float)calDims.Height;

                depthPixelBuffer = depthData.DepthDataMap;
                vf.depthWidth = (int)depthPixelBuffer.Width;
                vf.depthHeight = (int)depthPixelBuffer.Height;
                vf.depthBytesPerRow = (int)depthPixelBuffer.BytesPerRow;
                depthPixelBuffer.Lock(CVOptionFlags.None);
                imagePixelBuffer.Lock(CVOptionFlags.None);
                vf.SetBuffers(depthPixelBuffer.BaseAddress,
                    sendImageData ? imagePixelBuffer.BaseAddress : IntPtr.Zero);
                depthPixelBuffer.Unlock(CVOptionFlags.None);
                imagePixelBuffer.Unlock(CVOptionFlags.None);
                vf.DownScaleYUV(downSampleAmt);
                dt.AddVideoFrame(timeStamp, vf);

                if (this.depthTexture == null)
                {
                    Console.WriteLine("Error creating depth texture: {0}", status);
                    return;
                }

                GL.BindTexture(this.depthTexture.Target, this.depthTexture.Name);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                depthPixelBuffer.Dispose();
                frame.CapturedDepthData.Dispose();
                hasDepth = true;
            }

            imagePixelBuffer.Dispose();
            frame.Dispose();
        }

        private static readonly Vector3[] _ArrayPosition = {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f)
        };

        private static readonly ushort[] _ArrayElems = {
            0, 1, 2, 2, 3, 0,
        };

        /// <summary>
        /// Vertex color array.
        /// </summary>
        private static readonly Vector3[] _ArrayTexCoord = {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
        };

        class ARDelegate : ARSessionDelegate
        {
            GLViewController v;
            public ARDelegate(GLViewController vc)
            {
                v = vc;
            }

            public override void WasInterrupted(ARSession session)
            {
                Console.WriteLine("WasInterrupted");
            }
            public override void DidFail(ARSession session, NSError error)
            {
                Console.WriteLine("DidFail");
            }
            public override void DidUpdateFrame(ARSession session, ARFrame frame)
            {
                v.DidUpdateFrame(session, frame);
            }

            public override void DidUpdateAnchors(ARSession session, ARAnchor[] anchors)
            {
                v.DidUpdateAnchors(session, anchors);
            }

            public override void CameraDidChangeTrackingState(ARSession session, ARCamera camera)
            {
                v.CameraDidChangeTrackingState(session, camera);
            }
        }
    }


}
