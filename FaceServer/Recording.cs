using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK;

namespace Dopple
{
    public class Recording
    {
        List<Frame> allFrames = new List<Frame>();
        List<Frame> completeFrames = new List<Frame>();

        public List<Frame> Frames { get {
                return this.showFramesWithoutDepth ?
                this.allFrames : this.completeFrames; 
                } }
        const int arFrmIdx = 0;
        const int vidIdx = 1;
        bool showFramesWithoutDepth = false;

        public string Name { get; set; }
        public event EventHandler<OnFrameProcessedArgs> OnFrameProcessed;
        public event EventHandler<OnMeshBuiltArgs> OnMeshBuilt;

        public float MaxDepthVal { get; } = 0;
        public float MinDepthVal { get; } = 0;
        public Recording(byte []data, Settings settings)
        {
            long currentReadOffset = 0;

            while (currentReadOffset < data.LongLength)
            {
                uint message = BitConverter.ToUInt32(data, (int)currentReadOffset);
                if (message <= 200)
                {
                    currentReadOffset += sizeof(Int32);
                    long dataSize = BitConverter.ToInt64(data, (int)currentReadOffset);
                    currentReadOffset += sizeof(long);
                    byte[] msgbytes = new byte[dataSize];
                    Buffer.BlockCopy(data, (int)currentReadOffset, msgbytes, 0, msgbytes.Length);
                    if (message == 104)
                    {
                        Frame frm = Frame.FromBytes(msgbytes);
                        frm.parentRecording = this;
                        this.allFrames.Add(frm);
                    }
                    currentReadOffset += dataSize;
                    UInt32 footer = BitConverter.ToUInt32(data, (int)currentReadOffset);
                    if (footer != 0xABCDEF12)
                        throw new Exception("Bad data format");
                    currentReadOffset += sizeof(Int32);
                }
                else if (message == 0xABCDEF12)
                {
                    break;
                }
                else
                {
                    throw new Exception("Bad data format");
                }
            }

            allFrames.Sort((a, b) => { return a.timeStamp.CompareTo(b.timeStamp); });
            int tsIdx = 0;
            for (int idx = 0; idx < allFrames.Count; ++idx)
            {
                allFrames[idx].idx = idx;
            }

            float recmin = 0, recmax = 0, recavg = 0, avgweight = 0;
            bool init = false;
            foreach (Frame st in this.allFrames)
            {
                if (st.vf.HasDepth)
                {
                    this.completeFrames.Add(st);
                }
            }

            int itidx = 0;
            foreach (Frame st in this.completeFrames)
            { 
                float minval, maxval, avgval;
                //st.vf.MaskDepth(0.1f, 0.8f);
                st.vf.GetDepthVals(out minval, out maxval, out avgval);
                //st.vf.VidEdge();
                //st.vf.Blur();
                if (!init)
                {
                    recmin = minval;
                    recmax = maxval;
                    recavg = avgval;
                    avgweight = 1;
                    init = true;
                }
                else
                {
                    recmin = Math.Min(minval, recmin);
                    recmax = Math.Max(maxval, recmax);
                    recavg += avgval;
                    avgweight += 1;
                }
                itidx++;
            }

            this.OnFrameProcessed += Recording_OnFrameProcessed;

            MinDepthVal = recmin;
            MaxDepthVal = recmax;
            recavg /= avgweight;
        }

        public void BuildMeshes(Settings settings)
        {
            Thread workerThread = new Thread(() =>
            {
                Matrix4 accumTransform = Matrix4.Identity;
                for (int i = 0; i < NumFrames(); ++i)
                {
                    Frames[i].BuildData(settings, null, null);
                    float score = 0;
                    if (i > 0)
                    {
                        AlignFrame(i - 1, i);
                    }
                    OnFrameProcessed(this, new OnFrameProcessedArgs(i, i + 1, NumFrames(), 0));
                }
            });
            workerThread.Start();
        }

        void AlignFrame(int b, int d)
        {
            PtCldAlignNative ptalign = new PtCldAlignNative(Frames[b].ptMesh, Frames[d].ptMesh);
            Matrix4 outTransform = Matrix4.Identity;
            while (ptalign.AlignStep(out outTransform) < 2) ;
            Frames[d].AlignmentTransform = outTransform;
            Frames[d].AlignmentToFrame = b;
            GC.Collect();
        }

        float GetAlignScore(int b, int d)
        {
            if (Frames[d].AlignmentTransform == null)
                return 0;
            PTCloudAlignScore alignScorer = new PTCloudAlignScore(Frames[b].ptMesh, Frames[d].ptMesh,
                Frames[d].AlignmentTransform.Value, b);
            float score = alignScorer.GetScore(Frames[d].AlignmentTransform.Value);
            GC.KeepAlive(alignScorer);
            GC.Collect();
            return score;
        }
        public void BuildMeshesThreaded(Settings settings)
        {
            int nThreads = 28;
            Thread[] workerThreads = new Thread[nThreads];
            for (int i = 0; i < nThreads; ++i)
            {
                workerThreads[i] = new Thread(() => { BuildPtClouds(this, settings); });
                workerThreads[i].Start();
            }
        }

        private void Recording_OnFrameProcessed(object sender, OnFrameProcessedArgs e)
        {
            if (e.processed == e.total)
            {
                OnMeshBuilt(this, new OnMeshBuiltArgs(this));
            }
        }

        public class LastBuiltInfo
        {
            public Vector3 minbnd;
            public Vector3 maxbnd;
            public int frame;
        }

        static int nFrameToProcess = -1;
        static int nCompleted = 0;
        static LastBuiltInfo maxFrameBuilt = null;
        List<Tuple<int, int>> alignRequests = new List<Tuple<int, int>>();

        static void BuildPtClouds(Recording rec, Settings settings)
        {
            int alignTo = 10;
            while (true)
            {
                int curFrame = Interlocked.Increment(ref nFrameToProcess);
                if (curFrame >= rec.NumFrames())
                    break;
                rec.Frames[curFrame].BuildData(settings, maxFrameBuilt?.minbnd,
                    maxFrameBuilt?.maxbnd);
                Vector3 minb, maxb;
                rec.Frames[curFrame].ptMesh.GetMinMax(out minb, out maxb);
                Matrix4 wm = rec.Frames[curFrame].ptMesh.worldMatrix.Gl;
                minb = Vector3.TransformPosition(minb, wm);
                maxb = Vector3.TransformPosition(maxb, wm);

                lock (rec.alignRequests)
                {
                    alignTo = ((curFrame - 2) / alignTo) * alignTo + 1;
                    rec.alignRequests.Add(new Tuple<int, int>(curFrame, alignTo));

                    if (maxFrameBuilt == null || curFrame > maxFrameBuilt.frame)
                    {
                        maxFrameBuilt = new LastBuiltInfo()
                        {
                            frame = curFrame,
                            minbnd = minb,
                            maxbnd = maxb
                        };
                    }
                }

                int completed = Interlocked.Increment(ref nCompleted);
                rec.OnFrameProcessed(rec, new OnFrameProcessedArgs(curFrame, completed, rec.NumFrames(), 0));
            }
        }

        static void DoAlignment(Recording rec, Settings settings)
        { 
            while (true)
            {
                int frameToAlign = -1;
                lock (rec.alignRequests)
                {
                    if (rec.alignRequests.Count > 0)
                    {
                    }
                    else
                        break;
                }
                rec.AlignFrame(frameToAlign - 1, frameToAlign);
                int completed = Interlocked.Increment(ref nCompleted);
                if (rec.OnFrameProcessed != null)
                    rec.OnFrameProcessed(rec, new OnFrameProcessedArgs(frameToAlign, completed, rec.NumFrames(), 0));
            }
        }

        public void RefreshCumulativeTransform(int idx)
        {
            if (this.Frames[idx].AlignmentToFrame < 0)
                return;

            if (idx == 0)
            {
                this.Frames[idx].CumulativeAlignTrans = Matrix4.Identity;
            }
            else if (this.Frames[idx].CumulativeAlignTrans == null)
            {
                RefreshCumulativeTransform(this.Frames[idx].AlignmentToFrame);
                this.Frames[idx].CumulativeAlignTrans = Matrix4.Mult(
                    this.Frames[idx].AlignmentTransform != null ?
                    this.Frames[idx].AlignmentTransform.Value :
                    Matrix4.Identity,
                    this.Frames[this.Frames[idx].AlignmentToFrame].CumulativeAlignTrans.Value);
            }


        }
        public Frame GetFrame(int idx)
        {
            return this.Frames[idx];
        }
        public double GetTimeStamp(int idx)
        {
            return this.Frames[idx].timeStamp;
        }
        public int NumFrames()
        {
            return this.Frames.Count;
        }
    }

    public class OnFrameProcessedArgs : EventArgs
    {
        public OnFrameProcessedArgs(int _curFrame, int _processed, int _total, float _score)
        {
            curFrame = _curFrame;
            processed = _processed;
            total = _total;
            score = _score;
        }

        public int curFrame;
        public int processed;
        public int total;
        public float score;
    }
    public class OnMeshBuiltArgs : EventArgs
    {
        public OnMeshBuiltArgs(Recording _recording)
        {
            recording = _recording;
        }

        public Recording recording;
    }

    public struct Settings
    {
        public float imageDepthMix;
        public Vector2 imageScl;
        public Vector2 depthScl;
        public Vector2 depthOffset;
        public Vector2 faceScl;
        public Vector2 faceTranslate;
        public Vector2 depthRange;
        public bool autoAlign;

        public Settings(float m)
        {
            imageDepthMix = m;
            imageScl = new Vector2(1, 1);
            depthScl = new Vector2(1, 1); // new Vector2(1.06168234f, 1.06504071f);
            depthOffset = new Vector2(0, 0); // new Vector2(-0.00508905947f, 0.0123239458f);
            faceScl = new Vector2(1, 1);
            faceTranslate = new Vector2(0, 0);
            depthRange = new Vector2(0.1f, 10);
            autoAlign = false;
        }
        public void Apply(VideoMesh vm)
        {
            vm.imageDepthMix = imageDepthMix;
            vm.depthScale = imageScl;
            vm.depthVals = depthRange;
            vm.depthScale = depthScl;
            vm.depthOffset = depthOffset;
        }
    }
}
