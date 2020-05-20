using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using OpenTK;
using System.Threading;
using Dopple;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Markup;
using System.Runtime.CompilerServices;

namespace Planes
{
    class DPEngine
    {
        [DllImport("ptslib.dll")]
        public static extern IntPtr CreatePtCloudAlign(IntPtr m_pts0, uint ptCount0, IntPtr m_pts1, uint ptCount1);

        [DllImport("ptslib.dll")]
        public static extern int GetNearest(IntPtr pts0, uint ptCount0, IntPtr pts1, uint ptCount1, IntPtr outMatches);

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
        public class Tracked
        {
            public int startFrame;
            public List<Feature> features = new List<Feature>();
            public Vector3 color;
        }

        public class Feature
        {
            public Feature(int frame) { frameIdx = frame; }
            public int frameIdx;
            public Vector2 pt;
            public Feature prev = null;
            public Feature next = null;
            public Vector3 color;
            public float dist;
        }

        public class Features
        {
            public KeyPoint[] keypoints;
            public Feature[] features;
            public Mat descriptors;
        }

        public List<Tracked> []FrameTracked;

        public Features[] FrameFeatures;

        SURF detector;
        BriefDescriptorExtractor extractor;
        BFMatcher matcher;
        public static Vector3[] Palette = new Vector3[64];

        public OpenCV()
        {
            this.detector = SURF.Create(hessianThreshold: 4000);
            this.extractor = BriefDescriptorExtractor.Create();
            this.matcher = new BFMatcher();
            App.Recording.OnFrameChanged += Recording_OnFrameChanged;
            Random r = new Random();
            for (int i = 0; i < Palette.Length; ++i)
            {
                Palette[i] = new Vector3((float)r.NextDouble(),
                    (float)r.NextDouble(), (float)r.NextDouble());
                float maxVal = Math.Max(Math.Max(Palette[i].X, Palette[i].Y), Palette[i].Z);
                Palette[i] /= maxVal;
            }
            LoadFrame();
        }

        private void Recording_OnFrameChanged(object sender, int e)
        {
            LoadFrame();
        }
        public class TrackedPt
        {
            public Vector2 pt;
            public Vector3 col;
        }


        int nextFrameToProcess = 0;
        int framesCompleted = 0;
        AutoResetEvent frameCompletedEvt = new AutoResetEvent(false);
        void ProcessFrame(object o)
        {
            while (true)
            {
                int frameIdx = Interlocked.Increment(ref nextFrameToProcess) - 1;
                if (frameIdx >= App.Recording.NumFrames)
                    break;
                Dopple.VideoFrame vf1 = App.Recording.Frames[frameIdx].vf;
                Mat img1 = MatFromVidFrame(vf1);
                Features features = new Features();
                features.keypoints = detector.Detect(img1);
                features.descriptors = new Mat();
                features.features = new Feature[features.keypoints.Length];
                float invW = 1.0f / vf1.imageWidth;
                float invH = 1.0f / vf1.imageHeight;

                for (int idx = 0; idx < features.features.Length; ++idx)
                {
                    features.features[idx] = new Feature(frameIdx);
                    features.features[idx].pt = new Vector2(features.keypoints[idx].Pt.Y * invW,
                        features.keypoints[idx].Pt.X * invH);
                }
                extractor.Compute(img1, ref features.keypoints, features.descriptors);
                this.FrameFeatures[frameIdx] = features;
                Interlocked.Increment(ref framesCompleted);
                frameCompletedEvt.Set();
            }
        }

        AutoResetEvent matchesReadyForFrame = new AutoResetEvent(false);
        void LoadFrame()
        {
            if (FrameFeatures == null)
            {
                FrameFeatures = new Features[App.Recording.NumFrames];

                Thread[] threads = new Thread[28];
                for (int idx = 0; idx < threads.Length; ++idx)
                {
                    threads[idx] = new Thread(new ParameterizedThreadStart(ProcessFrame));
                    threads[idx].Start();
                }

                matchesReadyForFrame.Reset();
                Thread fmThread = new Thread(new ParameterizedThreadStart(FindMatches));
                fmThread.Start();
                matchesReadyForFrame.WaitOne();
            }
        }

        void FindMatches(object o)
        {
            for (int frameIdx = 0; frameIdx < this.FrameFeatures.Length - 1; ++frameIdx)
            {
                while ((this.FrameFeatures[frameIdx] == null ||
                    this.FrameFeatures[frameIdx + 1] == null) &&
                    framesCompleted < App.Recording.NumFrames)
                    frameCompletedEvt.WaitOne();

                if (this.FrameFeatures[frameIdx] != null &&
                    this.FrameFeatures[frameIdx + 1] != null)
                {
                    Features f0 = this.FrameFeatures[frameIdx];
                    Features f1 = this.FrameFeatures[frameIdx + 1];
                    var matches = matcher.KnnMatch(f0.descriptors, f1.descriptors, 2);

                    var singleMatches = matches.Where(m => m.Length == 1).ToList();
                    var otherMatches = matches.Where(m => m.Length > 1).OrderBy(m => m[0].Distance / m[1].Distance).ToList();

                    var bestMatches = singleMatches.Select(m => m[0]).Concat(otherMatches.Select(m => m[0])).ToList();

                    List<Point2f> mpts0 = new List<Point2f>();
                    List<Point2f> mpts1 = new List<Point2f>();

                    List<Tuple<float, int>> distances = new List<Tuple<float, int>>();
                    foreach (var m in bestMatches)
                    {
                        Vector2 p0 = f0.features[m.QueryIdx].pt;
                        mpts0.Add(new Point2f(p0.X, p0.Y));
                        Vector2 p1 = f1.features[m.TrainIdx].pt;
                        mpts1.Add(new Point2f(p1.X, p1.Y));

                        float dist = (f0.features[m.QueryIdx].pt - f1.features[m.TrainIdx].pt).Length;
                        distances.Add(new Tuple<float, int>(dist, m.QueryIdx));
                        f0.features[m.QueryIdx].next = f1.features[m.TrainIdx];
                        f1.features[m.TrainIdx].prev = f0.features[m.QueryIdx];
                    }

                    distances.Sort((a, b) => { return a.Item1.CompareTo(b.Item1); });

                    float tot = distances.Count;
                    int didx = 0;
                    foreach (var d in distances)
                    {
                        f0.features[d.Item2].dist = (float)(didx++) / tot;
                    }

                    /*
                    List<byte> outliers = new List<byte>(mpts1.Count);
                    OutputArray outputArray = OutputArray.Create(outliers);
                    Cv2.FindHomography(InputArray.Create(mpts0), InputArray.Create(mpts1), HomographyMethods.Ransac,
                        1, outputArray);

                    int midx = 0;
                    int outt = 0;
                    foreach (var m in bestMatches)
                    {
                        if (outliers[midx] == 1)
                        {
                            f0.features[m.QueryIdx].next = f1.features[m.TrainIdx];
                            f1.features[m.TrainIdx].prev = f0.features[m.QueryIdx];
                        }
                        else
                        {
                            outt++;
                        }
                        midx++;
                    }

                    Debug.WriteLine($"{outt} / {bestMatches.Count}");*/
                    /*
                    Vector3 c0 = new Vector3(1, 0, 0);
                    Vector3 c1 = new Vector3(0, 0, 1);
                    foreach (var m in bestMatches)
                    {
                        float dist = (f0.features[m.QueryIdx].pt - f1.features[m.TrainIdx].pt).Length;
                    }*/
                }

                if (frameIdx == App.Recording.CurrentFrameIdx)
                    matchesReadyForFrame.Set();
            }


            List<Tracked> allTracked = new List<Tracked>();
            int colIdx = 0;
            for (int frameIdx = 0; frameIdx < this.FrameFeatures.Length - 1; ++frameIdx)
            {
                Features f0 = this.FrameFeatures[frameIdx];
                foreach (var f in f0.features)
                {
                    if (f.prev == null && f.next != null)
                    {
                        List<Feature> features = new List<Feature>();
                        Feature cf = f;
                        float maxDist = 0;
                        while (cf != null)
                        {
                            features.Add(cf);
                            if (cf.next != null)
                                maxDist = Math.Max(maxDist, (cf.next.pt - cf.pt).Length);
                            cf = cf.next;                            
                        }
                        if (features.Count >= 5)
                        {
                            Tracked tracked = new Tracked();
                            tracked.color = Palette[(colIdx++) % 64];
                            tracked.startFrame = frameIdx;
                            tracked.features = features;
                            allTracked.Add(tracked);
                        }
                    }
                }
            }

            FrameTracked = new List<Tracked>[App.Recording.NumFrames];
            for (int frameIdx = 0; frameIdx < this.FrameFeatures.Length; ++frameIdx)
            {
                FrameTracked[frameIdx] = new List<Tracked>();
            }

            foreach (Tracked t in allTracked)
            {
                foreach (var f in t.features)
                {
                    FrameTracked[f.frameIdx].Add(t);
                }
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
