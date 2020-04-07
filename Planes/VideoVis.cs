using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;

namespace Planes
{
    class VideoVis
    {
        private Program _Program;
        private VertexArray vaScreenQuad;
        private TextureYUV _ImageTexture;
        private TextureFloat _DepthTexture;
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


        public VideoVis()
        {
            App.Recording.OnFrameChanged += ActiveRecording_OnFrameChanged;
            _Program = Program.FromFiles("VidShader.vert", "VidShader.frag");
            vaScreenQuad = new VertexArray(_Program, _ArrayPosition, _ArrayElems, _ArrayTexCoord, null);
            _ImageTexture = new TextureYUV();
            _DepthTexture = new TextureFloat();
        }


        bool hasNewFrame = true;
        private void ActiveRecording_OnFrameChanged(object sender, int e)
        {
            hasNewFrame = true;
        }


        public void Render()
        {
            if (hasNewFrame)
            {
                Dopple.VideoFrame vf = App.Recording.CurrentFrame.vf;
                _ImageTexture.LoadImageFrame(vf.ImageWidth, vf.ImageHeight,
                    vf.imageData);
                _DepthTexture.LoadDepthFrame(vf.DepthWidth, vf.DepthHeight,
                    vf.depthData);
                hasNewFrame = false;
            }
            _Program.Use(0);

            _Program.Set1("hasDepth", 0);
            _Program.Set1("depthSampler", (int)2);
            _Program.Set1("ySampler", (int)0);
            _Program.Set1("uvSampler", (int)1);
            _ImageTexture.BindToIndex(0, 1);
            _DepthTexture.BindToIndex(2);

            // Compute the model-view-projection on CPU
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 1, 0, 1, 1, 0);
            Matrix4 modelview = Matrix4.CreateTranslation(-0.5f, -0.5f, 0) * Matrix4.CreateRotationZ(-(float)Math.PI / 2.0f) *
                Matrix4.CreateTranslation(0.5f, 0.5f, 0);

            Matrix4 matWorldViewProj = modelview * projection;
            // Set uniform state
            _Program.SetMat4("uMVP", ref matWorldViewProj);
            // Use the vertex array
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            vaScreenQuad.Draw();
        }
    }
}
