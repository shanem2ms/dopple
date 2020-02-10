using System;
using OpenTK;
using OpenTK.Graphics.ES30;
using GLObjects;

namespace Dopple
{
    public class VideoMesh
    {
        /// <summary>
        /// The program used for drawing the triangle.
        /// </summary>
        private Program _Program;

        /// <summary>
        /// The vertex arrays used for drawing the triangle.
        /// </summary>
        private VertexArray vaScreenQuad;
        private TextureYUV _ImageTexture;
        private TextureFloat _DepthTexture;

        public Vector2 depthScale = new Vector2(1, 1);
        public Vector2 depthOffset = new Vector2(0, 0);
        public float imageDepthMix = 0;
        public Vector2 depthVals = new Vector2(0, 1);
        bool hasDepth = true;

        Frame currentFrame;
        bool hasNewFrame = false;
        public Frame CurrentFrame
        {
            get { return this.currentFrame; }
            set { this.currentFrame = value; hasNewFrame = true; }
        }

        public VideoMesh()
        {
            _Program = Program.FromFiles("VidShader.vert", "VidShader.frag");
            vaScreenQuad = new VertexArray(_Program, _ArrayPosition, _ArrayElems, _ArrayTexCoord, null);
            _ImageTexture = new TextureYUV();
            _DepthTexture = new TextureFloat();
        }

        public void Render(float imageDepthBlend)
        {
            if (this.currentFrame == null)
                return;
            // Select the program for drawing
            GL.UseProgram(_Program.ProgramName);

            if (hasNewFrame)
            {
                VideoFrame vf = this.currentFrame.vf;
                _ImageTexture.LoadImageFrame(vf);
                this.hasDepth = vf.DepthWidth > 0;
                if (hasDepth)
                    _DepthTexture.LoadDepthFrame(vf);
                hasNewFrame = false;
            }

            _Program.Set1("depthSampler", (int)2);
            _Program.Set1("ySampler", (int)0);
            _Program.Set1("uvSampler", (int)1);
            _Program.Set1("imageDepthMix", imageDepthBlend);
            _Program.Set2("depthVals", depthVals);
            _Program.Set1("hasDepth", hasDepth ? 1 : 0);
            _Program.Set2("depthScale", depthScale);
            _Program.Set2("depthOffset", depthOffset);

            _ImageTexture.BindToIndex(0, 1);
            _DepthTexture.BindToIndex(2);

            // Compute the model-view-projection on CPU
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 1, 0, 1, 1, 0);
            Matrix4 modelview = Matrix4.CreateTranslation(-0.5f, -0.5f, 0) * Matrix4.CreateRotationZ(-(float)Math.PI / 2.0f) *
                Matrix4.CreateTranslation(0.5f, 0.5f, 0);

            Matrix4 matWorldViewProj = modelview * projection;
            // Set uniform state
            GL.UniformMatrix4(_Program.LocationMVP, false, ref matWorldViewProj);
            // Use the vertex array
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            vaScreenQuad.Draw();
        }

        private static readonly Vector3[] _ArrayPosition = new Vector3[] {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f)
        };

        private static readonly ushort[] _ArrayElems = new ushort[]
        {
            0, 1, 2, 2, 3, 0,
        };

        /// <summary>
        /// Vertex color array.
        /// </summary>
        private static readonly Vector3[] _ArrayTexCoord = new Vector3[] {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
        };

        public void Dispose()
        {
            _Program?.Dispose();
            vaScreenQuad?.Dispose();
        }
    }
}
