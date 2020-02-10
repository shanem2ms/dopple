using System;
using AVFoundation;
using Foundation;
using UIKit;
using CoreVideo;
using CoreFoundation;
using System.Collections.Generic;
using GLObjects;
using System.Linq;
using CoreGraphics;
using OpenGLES;
using GLKit;
using OpenTK.Graphics.ES20;
using OpenTK;

namespace Dopple
{    public partial class DualEyeViewController : GLKViewController
    {

        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray vaScreenQuad;

        private int _ySamplerLoc;
        private int _uvSamplerLoc;
        private int _imgScaleLoc;
        private int _imgOffsetLoc;

        double frameTimeStamp = 0;

        private AVCaptureSession session;
        AVCaptureVideoDataOutput colorOutput;

        private EAGLContext context;
        private CVOpenGLESTexture lumaTexture, chromaTexture;
        public CVOpenGLESTextureCache VideoTextureCache { get; private set; }
        DataTransmit dt = new DataTransmit();

        private Vector3[] _ArrayPosition;

        private ushort[] _ArrayElems;

        private Vector3[] _ArrayTexCoord;

        #region Constructors
        protected DualEyeViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        #endregion

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
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
            SetupAVCapture(AVCaptureSession.PresetiFrame1280x720);
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

            if (dstR == 0)
                return v;

            // distance or radius of src image (with formula)
            double srcR = (paramA * dstR * dstR * dstR + paramB * dstR * dstR + paramC * dstR + paramD) * dstR;

            // comparing old and new distance to get factor
            double factor = Math.Abs(dstR / srcR);
            factor = Math.Pow(factor, 4);
            // coordinates in source image

            return new Vector2(v.X * (float)factor, v.Y * (float)factor);
        }

        void BuildBarrelDistortionMesh()
        {
            List<Vector3> posList = new List<Vector3>();
            List<Vector3> texCoord = new List<Vector3>();
            int seg = 10;
            float seglen = 1.0f / seg;
            for (int y = -seg; y <= seg; ++y)
            {
                for (int x = -seg; x <= seg; ++x)
                {
                    Vector2 tx = new Vector2(x * seglen, y * seglen);
                    Vector2 v = Barrel(tx);
                    tx += new Vector2(1, 1);
                    tx *= 0.5f;
                    v += new Vector2(1, 1);
                    v *= 0.5f;

                    posList.Add(new Vector3(v.X, v.Y, 0));
                    texCoord.Add(new Vector3(tx.X, tx.Y, 0));
                }
            }

            int stride = seg * 2 + 1;
            List<ushort> indices = new List<ushort>();
            for (int y = 0; y < stride - 1; ++y)
            {
                for (int x = 0; x < stride - 1; ++x)
                {
                    int r0c0 = y * stride + x;
                    int r0c1 = y * stride + (x + 1);
                    int r1c0 = (y + 1) * stride + x;
                    int r1c1 = (y + 1) * stride + (x + 1);
                    indices.Add((ushort)r0c0);
                    indices.Add((ushort)r0c1);
                    indices.Add((ushort)r1c1);
                    indices.Add((ushort)r0c0);
                    indices.Add((ushort)r1c1);
                    indices.Add((ushort)r1c0);
                }
            }


            _ArrayPosition = posList.ToArray();
            _ArrayTexCoord = texCoord.ToArray();
            _ArrayElems = indices.ToArray();
        }

        private void SetupGL()
        {
            EAGLContext.SetCurrentContext(this.context);
            _Program = new Program("Shaders/DualEye.vert", "Shaders/DualEye.frag");
            ErrorCode cd = GL.GetErrorCode();
            _ySamplerLoc = GL.GetUniformLocation(_Program.ProgramName, "ySampler");
            _uvSamplerLoc = GL.GetUniformLocation(_Program.ProgramName, "uvSampler");
            cd = GL.GetErrorCode();
            _imgScaleLoc = GL.GetUniformLocation(_Program.ProgramName, "imgScale");
            _imgOffsetLoc = GL.GetUniformLocation(_Program.ProgramName, "imgOffset");
            cd = GL.GetErrorCode();

            if (_ArrayPosition == null)
            {
                BuildBarrelDistortionMesh();
            }
            vaScreenQuad = new VertexArray(_ArrayPosition, _ArrayElems, _ArrayTexCoord, null);
            cd = GL.GetErrorCode();

            if ((this.VideoTextureCache = CVOpenGLESTextureCache.FromEAGLContext(this.context)) == null)
            {
                Console.WriteLine("Could not create the CoreVideo TextureCache");
                return;
            }
        }

        public override void DrawInRect(GLKView view, CGRect rect)
        {
            EAGLContext.SetCurrentContext(this.context);
            GL.ClearColor(0, 0, 1, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);

            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);
            vaScreenQuad.Bind(_Program);

            GL.Uniform1(this._ySamplerLoc, (int)0);
            GL.Uniform1(this._uvSamplerLoc, (int)1);

            // Compute the model-view-projection on CPU
            float eyeScale = 0.75f;
            Matrix4 mvProj = 
                Matrix4.Mult(
                Matrix4.Mult(Matrix4.Scale(eyeScale, 2 * eyeScale, 1), Matrix4.CreateTranslation(-(eyeScale + xEyeCenterOffset), -eyeScale, 0.5f)),
                Matrix4.Scale(1, -1, 1));
            // Set uniform state
            GL.UniformMatrix4(_Program.LocationMVP, false, ref mvProj);
            // Use the vertex array
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            GL.DrawElements(BeginMode.Triangles, _ArrayElems.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);


            // Compute the model-view-projection on CPU
            mvProj =
                Matrix4.Mult(
                Matrix4.Mult(Matrix4.Scale(eyeScale, 2 * eyeScale, 1), Matrix4.CreateTranslation(xEyeCenterOffset, -eyeScale, 0.5f)),
                Matrix4.Scale(1, -1, 1));
            // Set uniform state
            GL.UniformMatrix4(_Program.LocationMVP, false, ref mvProj);
            // Use the vertex array
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            GL.DrawElements(BeginMode.Triangles, _ArrayElems.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        class ActiveTouch
        {
            public CGPoint start;
            public CGPoint current;
        }

        float xEyeCenterOffset = 0;
        float xEyeCenterOffsetDn = 0;
        Dictionary<UITouch, ActiveTouch> activeTouches = new Dictionary<UITouch, ActiveTouch>();

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                ActiveTouch at = new ActiveTouch();
                at.start = touch.LocationInView(touch.View);
                activeTouches.Add(touch, at);
                xEyeCenterOffsetDn = xEyeCenterOffset;
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                ActiveTouch at = activeTouches[touch];
                CGPoint pt = touch.LocationInView(touch.View);
                at.current = pt;
                xEyeCenterOffset = xEyeCenterOffsetDn + (float)(at.current.X - at.start.X) / (float)this.View.Frame.Width;
            }          
        }
        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            foreach (UITouch touch in touches.ToArray<UITouch>())
            {
                activeTouches.Remove(touch);
            }

        }
        private void SetupAVCapture(NSString sessionPreset)
        {
            if ((this.VideoTextureCache = CVOpenGLESTextureCache.FromEAGLContext(this.context)) == null)
            {
                Console.WriteLine("Could not create the CoreVideo TextureCache");
                return;
            }

            this.session = new AVCaptureSession();
            this.session.BeginConfiguration();

            // Preset size
            this.session.SessionPreset = sessionPreset;

            foreach (AVCaptureDevice d in AVCaptureDevice.Devices)
            {
                Console.WriteLine(d.Description);
                var t = d.DeviceType;
            }
            // Input device
            var videoDevice = AVCaptureDevice.GetDefaultDevice(
                AVCaptureDeviceType.BuiltInWideAngleCamera,
                AVMediaTypes.Video.GetConstant(),
                AVCaptureDevicePosition.Back);

            if (videoDevice == null)
            {
                Console.WriteLine("No video device");
                return;
            }

            AVCaptureDeviceFormat []highspeedFormats = 
                videoDevice.Formats.Where(f => f.VideoSupportedFrameRateRanges[0].MaxFrameRate == 240).ToArray();

            var myFormat = highspeedFormats[2];
            videoDevice.LockForConfiguration(out NSError err);
            videoDevice.ActiveFormat = myFormat;
            videoDevice.ActiveVideoMinFrameDuration = myFormat.VideoSupportedFrameRateRanges[0].MinFrameDuration;
            videoDevice.ActiveVideoMaxFrameDuration = myFormat.VideoSupportedFrameRateRanges[0].MaxFrameDuration;
            videoDevice.UnlockForConfiguration();
            Console.WriteLine("Active Format: " + videoDevice.ActiveFormat.Description);

            var input = new AVCaptureDeviceInput(videoDevice, out NSError error);
            if (error != null)
            {
                Console.WriteLine("Error creating video capture device");
                return;
            }


            this.session.AddInput(input);

            // Create the output device
            this.colorOutput = new AVCaptureVideoDataOutput();
            this.colorOutput.AlwaysDiscardsLateVideoFrames = true;

            if (this.session.CanAddOutput(colorOutput))
            {
                this.session.AddOutput(colorOutput);
            }
            

            var outputSynchronizer = new AVCaptureDataOutputSynchronizer(new AVCaptureOutput[] {  colorOutput });
            outputSynchronizer.SetDelegate(new AVCaptureDelegate(this), DispatchQueue.MainQueue);
            

            this.session.CommitConfiguration();
            this.session.StartRunning();
        }

        public void DidOutputSynchronizedDataCollection(AVCaptureDataOutputSynchronizer synchronizer, AVCaptureSynchronizedDataCollection synchronizedDataCollection)
        {
            CoreMedia.CMSampleBuffer sampleData = null;
            CVPixelBuffer samplePixelBuffer = null;
            AVCaptureSynchronizedSampleBufferData sampleBufferData = null;

            try
            {

                sampleBufferData = (AVCaptureSynchronizedSampleBufferData)
                    synchronizedDataCollection.GetSynchronizedData(this.colorOutput);

                if (sampleBufferData == null)
                    return;

                if (sampleBufferData.SampleBufferWasDropped)
                    Console.WriteLine(sampleBufferData.DroppedReason);


                if (sampleBufferData.SampleBufferWasDropped)
                    return;

                sampleData = sampleBufferData.SampleBuffer;
                if (sampleData != null)
                    samplePixelBuffer = sampleData.GetImageBuffer() as CVPixelBuffer;

                CGColorSpace colorspace = samplePixelBuffer.ColorSpace;

                var pixelBuffer = samplePixelBuffer;
                int width = (int)pixelBuffer.Width;
                int height = (int)pixelBuffer.Height;

                this.CleanupTextures();

                // Y-plane
                GL.ActiveTexture(TextureUnit.Texture0);
                All re = (All)0x1903; // GL_RED_EXT, RED component from ARB OpenGL extension


                this.lumaTexture = this.VideoTextureCache.TextureFromImage(pixelBuffer, true, re, width, height, re, DataType.UnsignedByte, 0, out CVReturn status);
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
                this.chromaTexture = VideoTextureCache.TextureFromImage(pixelBuffer, true, re, width / 2, height / 2, re, DataType.UnsignedByte, 1, out status);
                if (this.chromaTexture == null)
                {
                    Console.WriteLine("Error creating chroma texture: {0}", status);
                    return;
                }

                GL.BindTexture(this.chromaTexture.Target, this.chromaTexture.Name);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

            }

            finally

            {
                if (samplePixelBuffer != null)
                    samplePixelBuffer.Dispose();

                if (sampleBufferData != null)
                    sampleBufferData.Dispose();

                synchronizedDataCollection.Dispose();
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

            {
                this.VideoTextureCache.Flush(CVOptionFlags.None);
            }


        }

        class AVCaptureDelegate : AVCaptureDataOutputSynchronizerDelegate
        {
            DualEyeViewController v;
            public AVCaptureDelegate(DualEyeViewController vc)
            {
                v = vc;
            }
            public override void DidOutputSynchronizedDataCollection(AVCaptureDataOutputSynchronizer synchronizer, AVCaptureSynchronizedDataCollection synchronizedDataCollection)
            {
                v.DidOutputSynchronizedDataCollection(synchronizer, synchronizedDataCollection);
            }
        }
    }
}