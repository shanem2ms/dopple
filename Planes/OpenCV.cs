using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using OpenTK;

namespace Planes
{
    class DPEngine
    {
        [DllImport("ptslib.dll")]
        public static extern IntPtr CreatePtCloudAlign(IntPtr m_pts0, uint ptCount0, IntPtr m_pts1, uint ptCount1);

        [DllImport("ptslib.dll")]
        public static extern int AlignStep(IntPtr aligner, IntPtr outmatrix);

        [DllImport("ptslib.dll")]
        public static extern void FreePtCloudAlign(IntPtr aligner);

        [DllImport("ptslib.dll")]
        public static extern void BestFit(IntPtr pts0, uint ptCount0, IntPtr pts1, uint ptCount1, IntPtr outTranslate,
            IntPtr outRotate);

        [DllImport("ptslib.dll")]
        public static extern void CalcScores();


        public static IntPtr AllocVec3Array(Vector3[] pos)
        {

            IntPtr mpts0 = Marshal.AllocHGlobal(pos.Length * 3 * sizeof(float));
            CopyVec3Array(pos, mpts0);
            return mpts0;
        }

        public static void CopyVec3Array(Vector3[] pos, IntPtr mpts0)
        {
            float[] vals = new float[pos.Length * 3];
            for (int idx = 0; idx < pos.Length; ++idx)
            {
                vals[idx * 3] = pos[idx].X;
                vals[idx * 3 + 1] = pos[idx].Y;
                vals[idx * 3 + 2] = pos[idx].Z;
            }
            Marshal.Copy(vals, 0, mpts0, vals.Length);
        }


        public static Matrix4 MatrixDToF(Matrix4d m)
        {
            return new Matrix4(Vector4DtoF(m.Row0),
                Vector4DtoF(m.Row1),
                Vector4DtoF(m.Row2),
                Vector4DtoF(m.Row3));
        }

        public static Vector4 Vector4DtoF(Vector4d v)
        {
            return new Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
        }
    }

    public class OpenCV
    {
        public class Match
        {
            public float dist3d;
            public float dist2d;
            public Vector2[] pts;
            public Vector3[] wspts;
            public Vector3 color;
        }

        public List<Match> ActiveMatches;

        public OpenCV()
        {
            App.Recording.OnFrameChanged += Recording_OnFrameChanged;
            LoadFrame(App.FrameDelta);
        }

        private void Recording_OnFrameChanged(object sender, int e)
        {
            //LoadFrame(App.FrameDelta);
        }

        void LoadFrame(int delta)
        {
            int frameIdx = App.Recording.CurrentFrameIdx;
            if (frameIdx >= App.Recording.NumFrames - delta)
                return;
            Dopple.VideoFrame vf1 = App.Recording.Frames[frameIdx].vf;
            Dopple.VideoFrame vf2 = App.Recording.Frames[frameIdx + delta].vf;
            Mat img1 = MatFromVidFrame(vf1);
            Mat img2 = MatFromVidFrame(vf2);
            var detector = SURF.Create(hessianThreshold: 4000);
            var keypoints1 = detector.Detect(img1);
            var keypoints2 = detector.Detect(img2);
            var extractor = BriefDescriptorExtractor.Create();

            var descriptors1 = new Mat();
            var descriptors2 = new Mat();
            extractor.Compute(img1, ref keypoints1, descriptors1);
            extractor.Compute(img2, ref keypoints2, descriptors2);

            // matching descriptors
            var matcher = new BFMatcher();
            var matches = matcher.KnnMatch(descriptors1, descriptors2, 2);
            var distMatches = matches.Where(m => m.Length > 1 && m[0].Distance * 2 < m[1].Distance).Select(m => m[0]);
            if (distMatches == null)
                return;
            Point2f[] pts1 = new Point2f[distMatches.Count()];
            Point2f[] pts2 = new Point2f[distMatches.Count()];

            ActiveMatches = new List<Match>(distMatches.Count());
            int idx = 0;
            float invWidth = 1.0f / vf1.ImageWidth * vf1.DepthWidth;
            float invHeight = 1.0f / vf1.ImageHeight * vf1.DepthHeight;

            var ptsDict1 = vf1.DepthPts;
            var ptsDict2 = vf2.DepthPts;
            Random r = new Random();
            foreach (var m in distMatches)
            {
                var p1 = keypoints1[m.QueryIdx].Pt;
                var p2 = keypoints2[m.TrainIdx].Pt;

                int d1x = (int)(p1.X * invHeight);
                int d1y = (int)(p1.Y * invWidth);
                int d2x = (int)(p2.X * invHeight);
                int d2y = (int)(p2.Y * invWidth);

                int offset = d1x * vf1.DepthWidth + d1y;
                if (ptsDict1.ContainsKey(offset) &&
                    ptsDict2.ContainsKey(offset) &&
                    ptsDict1[offset].pt.Z > -5 &&
                    ptsDict2[offset].pt.Z > -5)
                {
                    ActiveMatches.Add(
                        new Match()
                        {
                            pts = new Vector2[2] { new Vector2(p1.X, p1.Y),
                                new Vector2(p2.X, p2.Y) },
                            wspts = new Vector3[2]
                            {
                                ptsDict1[offset].pt,
                                ptsDict2[offset].pt
                            },
                            dist2d = (new Vector2(p1.X, p1.Y) - new Vector2(p2.X, p2.Y)).Length,
                            dist3d = (ptsDict1[offset].pt - ptsDict2[offset].pt).Length,
                            color = new Vector3(0.5f + (float)r.NextDouble() * 0.5f,
                                    0.5f + (float)r.NextDouble() * 0.5f,
                                    0.5f + (float)r.NextDouble() * 0.5f)
                        });
                }
            }

            Vector3[] v0 = new Vector3[ActiveMatches.Count];
            Vector3[] v1 = new Vector3[v0.Length];
            IntPtr v1Ptr = Marshal.AllocHGlobal(v0.Length * 3 * sizeof(float));
            IntPtr v2Ptr = Marshal.AllocHGlobal(v1.Length * 3 * sizeof(float));
            IntPtr translatePtr = Marshal.AllocHGlobal(sizeof(float) * 3);
            IntPtr rotatePtr = Marshal.AllocHGlobal(sizeof(float) * 4);

            float totalLen = 0;
            for (int i = 0; i < v0.Length; ++i)
            {
                Vector3 vp0 = ActiveMatches[i].wspts[0];
                Vector3 vp1 = ActiveMatches[i].wspts[1];
                vp0.Z = (vp0.Z + vp1.Z) * 0.5f;
                vp1.Z = vp0.Z;
                v0[i] = vp0;
                v1[i] = vp1;
                totalLen += (vp1 - vp0).Length;
            }

            DPEngine.CopyVec3Array(v0, v1Ptr);
            DPEngine.CopyVec3Array(v1, v2Ptr);
            DPEngine.BestFit(v1Ptr, (uint)v0.Length, v2Ptr, (uint)v1.Length, translatePtr,
                rotatePtr);
            Vector3 translate = (Vector3)Marshal.PtrToStructure(translatePtr, typeof(Vector3));
            Vector4 rotate = (Vector4)Marshal.PtrToStructure(rotatePtr, typeof(Vector4));

            Vector3 axis = new Vector3(rotate).Normalized();
            float angle = rotate.W;
            Matrix4 mat =
                Matrix4.CreateFromQuaternion(Quaternion.FromAxisAngle(axis, angle)) * 
                Matrix4.CreateTranslation(new Vector3(translate));

            float betterLen = 0;
            for (int i = 0; i < v0.Length; ++i)
            {
                Vector3 v0b = Vector3.TransformPosition(v0[i], mat);
                betterLen += (v0b - v1[i]).Length;
            }

            Marshal.FreeHGlobal(translatePtr);
            Marshal.FreeHGlobal(rotatePtr);
            Marshal.FreeHGlobal(v1Ptr);
            Marshal.FreeHGlobal(v2Ptr);
            if (false)
            {
                var imgMatches = new Mat();
                Cv2.DrawMatches(img1, keypoints1, img2, keypoints2, distMatches, imgMatches);
                Cv2.ImShow("source", imgMatches);
            }
        }

        static Mat MatFromVidFrame(Dopple.VideoFrame vf)
        {
            int w = vf.ImageWidth;
            int h = vf.imageHeight;
            byte[] d = vf.imageData;
            byte[,] data = new byte[w, h];

            int row = 0;
            int column = 0;

            int len = w * h;
            for (int i = 0; i < len; i++)
            {
                row = i / w;
                column = i % w;
                data[column, row] = d[i];
            }
            return Mat.FromArray<byte>(data);
        }
    }
}
