using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;

namespace Planes
{
    class VideoVis
    {
        Vector2 depthVals;
        
        private Program _Program;
        private VertexArray vaScreenQuad;
        private TextureYUV _ImageTexture;
        private TextureFloat _DepthTexture;
        TextureR8 markersTex;

        int frameOffset;
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


        public VideoVis(int _frameOffset)
        {
            frameOffset = _frameOffset;
            App.Recording.OnFrameChanged += ActiveRecording_OnFrameChanged;
            _Program = Registry.Programs["vid"];
            vaScreenQuad = new VertexArray(_Program, _ArrayPosition, _ArrayElems, _ArrayTexCoord, null);
            _ImageTexture = new TextureYUV();
            _DepthTexture = new TextureFloat();
            markersTex = new TextureR8();
            float invmax = 1.0f /  App.Recording.MaxDepthVal;
            float invmin = 1.0f / App.Recording.MinDepthVal;
            depthVals.X = invmax;
            depthVals.Y = 1.0f / (invmin - invmax);
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
                int curFrame = App.Recording.CurrentFrameIdx + this.frameOffset;
                if (curFrame >= App.Recording.Frames.Count)
                    curFrame = App.Recording.Frames.Count - 1;
                Dopple.VideoFrame vf = App.Recording.Frames[curFrame].vf;
                _ImageTexture.LoadImageFrame(vf.ImageWidth, vf.ImageHeight,
                    vf.imageData);
                _DepthTexture.LoadDepthFrame(vf.DepthWidth, vf.DepthHeight,
                    vf.depthData);
                byte[] data = new byte[1024 * 1024];
                for (int i = 0; i < data.Length; ++i)
                { data[i] = 0; }
                float invWidth = 1.0f / vf.ImageWidth * 1024.0f;
                float invHeight = 1.0f / vf.ImageHeight * 1024.0f;
                foreach (var match in App.OpenCV.ActiveMatches)
                {
                    Vector2 pt = match.pts[this.frameOffset];
                    int w = (int)(pt.X * invHeight);
                    int h = (int)(pt.Y * invWidth);
                    int r = 2;
                    for (int xv = w - r; xv < w + r; ++xv)
                    {
                        for (int yv = h - r; yv < h + r; ++yv)
                        {
                            if (xv < 0 || yv < 0 || xv >= 1024 || yv >= 1024)
                                continue;
                            data[xv * 1024 + yv] = 255;
                        }
                    }
                }
                markersTex.LoadData(1024, 1024, data);
                hasNewFrame = false;
            }
            _Program.Use(0);

            _Program.Set1("hasDepth", 0);
            _Program.Set1("depthSampler", (int)2);
            _Program.Set1("ySampler", (int)0);
            _Program.Set1("uvSampler", (int)1);
            _Program.Set2("depthVals", depthVals);
            _ImageTexture.BindToIndex(0, 1);
            _DepthTexture.BindToIndex(2);
            _Program.Set1("markerTex", (int)3);
            markersTex.BindToIndex(3);
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
