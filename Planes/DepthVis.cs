using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Windows.Documents;
using System.Linq;
using Dopple;

namespace Planes
{
    class DepthVis
    {
        Vector2 depthRange;

        private Program _Program;
        private VertexArray vaScreenQuad;
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


        public DepthVis(int _frameOffset)
        {
            frameOffset = _frameOffset;
            App.Recording.OnFrameChanged += ActiveRecording_OnFrameChanged;
            _Program = Registry.Programs["depth"];
            vaScreenQuad = new VertexArray(_Program, _ArrayPosition, _ArrayElems, _ArrayTexCoord, null);
            _DepthTexture = new TextureFloat();
            markersTex = new TextureR8();
            float invmax = 1.0f / App.Recording.MaxDepthVal;
            float invmin = 1.0f / App.Recording.MinDepthVal;
            depthRange.X = invmax;
            depthRange.Y = 1.0f / (invmin - invmax);
            App.Settings.OnSettingsChanged += Settings_OnSettingsChanged;
        }

        bool hasNewFrame = true;
        private void Settings_OnSettingsChanged(object sender, EventArgs e)
        {
            hasNewFrame = true;
        }

        private void ActiveRecording_OnFrameChanged(object sender, int e)
        {
            hasNewFrame = true;
        }


        public void Render(Matrix4 viewProj)
        {
            if (hasNewFrame && App.Recording.Frames.Count > 0)
            {
                int curFrame = App.Recording.CurrentFrameIdx + this.frameOffset;
                if (curFrame >= App.Recording.Frames.Count)
                    curFrame = App.Recording.Frames.Count - 1;
                Dopple.VideoFrame vf = App.Recording.Frames[curFrame].vf;
                float[] depthIn = vf.GetDepthVals();
                float[] depthVals =
                    VideoFrame.GetDepthInv(depthIn, vf.DepthWidth, vf.DepthHeight, out _);

                _DepthTexture.LoadDepthFrame(vf.DepthWidth, vf.DepthHeight, depthVals);

                float max = depthVals.Max();
                float min = depthVals.Min();
                //var dvValid = depthVals.Where(f => !float.IsNaN(f) && !float.IsInfinity(f));

                this.depthRange = new Vector2(min, max);
                hasNewFrame = false;
            }

            _Program.Use(0);

            _Program.Set1("hasDepth", 0);            
            _Program.Set1("depthSampler", (int)0);
            _Program.Set2("depthRange", depthRange);
            _DepthTexture.BindToIndex(0);
            _Program.Set1("markerTex", (int)3);
            markersTex.BindToIndex(3);
            // Set uniform state
            _Program.SetMat4("uMVP", ref viewProj);
            // Use the vertex array
            // Draw triangle
            // Note: vertex attributes are streamed from GPU memory
            vaScreenQuad.Draw();
        }
    }
}
