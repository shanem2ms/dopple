using System;
using Dopple;
using OpenTK;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Planes
{
    public class PtCloudAligner
    {
        IntPtr aligner = IntPtr.Zero;

        public PtCloudAligner()
        {
            //App.Recording.OnFrameChanged += Recording_OnFrameChanged;
            //LoadFrame(App.FrameDelta);
        }

        private void Recording_OnFrameChanged(object sender, int e)
        {
            LoadFrame(App.FrameDelta);
        }

        public Tuple<int, int>[] Matches;
        
        void LoadFrame(int delta)
        {
            int frameIdx = App.Recording.CurrentFrameIdx;
            if (frameIdx >= App.Recording.NumFrames - delta)
                return;
            VideoFrame vf0 = App.Recording.Frames[frameIdx].vf;
            VideoFrame vf1 = App.Recording.Frames[frameIdx + delta].vf;
            var depthPts0 = vf0.CalcDepthPoints();
            var depthPts1 = vf1.CalcDepthPoints();
            Vector3[] pts0 = depthPts0.Select(kv => kv.Value.pt).ToArray();
            int[] ptidx0 = depthPts0.Select(kv => kv.Key).ToArray();
            Vector3[] pts1 = depthPts1.Select(kv => kv.Value.pt).ToArray();
            int[] ptidx1 = depthPts1.Select(kv => kv.Key).ToArray();
            IntPtr mpts0 = DPEngine.AllocVec3Array(pts0);
            IntPtr mpts1 = DPEngine.AllocVec3Array(pts1);

            IntPtr outInts = Marshal.AllocHGlobal((pts0.Length + pts1.Length) * 4);
            int nMatches = DPEngine.GetNearest(mpts0, (uint)pts0.Length, mpts1, (uint)pts1.Length, outInts);

            int []matches = new int[nMatches * 2];
            Marshal.Copy(outInts, matches, 0, matches.Length);

            List<Tuple<int, int>> mtuples = new List<Tuple<int, int>>();
            for (int idx = 0; idx < matches.Length; idx += 2)
            {
                mtuples.Add(new Tuple<int, int>(ptidx0[matches[idx]],
                    ptidx1[matches[idx + 1]]));
            }

            Matches = mtuples.ToArray();
            Marshal.FreeHGlobal(outInts);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);

            
        }

        public Matrix4 AlignedMatrix { get; set; }

        public Matrix4 Align()
        {
            Matrix4 outTransform = Matrix4.Identity;
            while (AlignStep(out outTransform) < 2);
            return outTransform;
        }

        public int AlignStep(out Matrix4 transform)
        {
            IntPtr mmatrix = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Matrix4)));
            int retval = DPEngine.AlignStep(this.aligner, mmatrix);
            transform = (Matrix4)Marshal.PtrToStructure(mmatrix, typeof(Matrix4));
            return retval;
        }

        ~PtCloudAligner()
        {
            DPEngine.FreePtCloudAlign(this.aligner);
        }
    }
}
