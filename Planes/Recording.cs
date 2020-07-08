using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK;
using System.ComponentModel;

namespace Dopple
{
    public class Recording : INotifyPropertyChanged
    {
        List<Frame> allFrames = new List<Frame>();
        List<Frame> completeFrames = new List<Frame>();

        public event EventHandler<int> OnFrameChanged;
        public event EventHandler<double> OnDownloadProgress;

        public int NumFrames => Frames.Count;
        int curFrameIdx = 0;
        public int CurrentFrameIdx
        {
            get => curFrameIdx;
            set
            {
                curFrameIdx = value;
                if (curFrameIdx < 0)
                    curFrameIdx = 0;
                if (curFrameIdx >= Frames.Count)
                    curFrameIdx = Frames.Count - 1;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFrameIdx"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFrame"));
                OnFrameChanged?.Invoke(this, curFrameIdx);
            }
        }

        System.Timers.Timer playTimer;
        bool isPlaying = false;
        public bool IsPlaying
        {
            get => isPlaying;
            set { 
                isPlaying = value; 
                if (isPlaying)
                {
                    playTimer = new System.Timers.Timer();
                    playTimer.Elapsed += PlayTimer_Elapsed;
                    playTimer.Interval = 1000 / 30.0;
                    playTimer.Start();
                }
                else
                {
                    playTimer.Stop();
                    playTimer.Elapsed -= PlayTimer_Elapsed;
                    playTimer = null;
                }
            }
        }

        private void PlayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CurrentFrameIdx = (CurrentFrameIdx + 1) % NumFrames;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFrameIdx"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFrame"));
        }

        static Recording live = null;
        static public Recording Live
        {

            get
            {
                if (live == null)
                {
                    live = new Recording();
                }
                return live;
            }
        }

        public string Name { get; }

        public Frame CurrentFrame => Frames[this.curFrameIdx];

        public List<Frame> Frames
        {
            get
            {
                return this.showFramesWithoutDepth ?
                this.allFrames : this.completeFrames;
            }
        }
        const int arFrmIdx = 0;
        const int vidIdx = 1;
        bool showFramesWithoutDepth = false;

        public event EventHandler<OnFrameProcessedArgs> OnFrameProcessed;
        public event EventHandler<OnMeshBuiltArgs> OnMeshBuilt;
        public event PropertyChangedEventHandler PropertyChanged;

        public float MaxDepthVal { get; } = 0;
        public float MinDepthVal { get; } = 0;

        TcpService tcpService = new TcpService();

        public Recording()
        {

            TcpService tcpService = new TcpService();
            tcpService.OnDataReceived += TcpService_OnDataReceived;
            tcpService.OnLiveFrame += TcpService_OnLiveFrame;
            tcpService.OnNewRecording += TcpService_OnNewRecording;
            tcpService.Start();

        }

        private void TcpService_OnNewRecording(object sender, OnNewRecordingArgs e)
        {
            throw new NotImplementedException();
        }

        private void TcpService_OnLiveFrame(object sender, OnLiveFrameArgs e)
        {
            this.Frames.Add(e.frame);
            this.curFrameIdx = this.Frames.Count - 1;
            OnFrameChanged?.Invoke(this, this.Frames.Count - 1);
        }

        private void TcpService_OnDataReceived(object sender, OnDataReceived e)
        {
            this.OnDownloadProgress?.Invoke(this, ((double)e.dataSoFar / (double)e.totalData));
        }

        public Recording(string name, byte[] data, Settings settings)
        {
            long currentReadOffset = 0;
            Name = name;
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
            this.OnFrameProcessed += Recording_OnFrameProcessed;
            recavg /= avgweight;

            this.curFrameIdx = 187;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NumFrames"));
        }

        public void BuildMeshes(Settings settings)
        {
            Thread workerThread = new Thread(() =>
            {
                Matrix4 accumTransform = Matrix4.Identity;
                for (int i = 0; i < NumFrames; ++i)
                {
                    Frames[i].BuildData(settings, null, null);
                    OnFrameProcessed(this, new OnFrameProcessedArgs(i, i + 1, NumFrames, 0));
                }
            });
            workerThread.Start();
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
                if (curFrame >= rec.NumFrames)
                    break;
                rec.Frames[curFrame].BuildData(settings, maxFrameBuilt?.minbnd,
                    maxFrameBuilt?.maxbnd);
                Vector3 minb, maxb;
                int completed = Interlocked.Increment(ref nCompleted);
                rec.OnFrameProcessed(rec, new OnFrameProcessedArgs(curFrame, completed, rec.NumFrames, 0));
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
                int completed = Interlocked.Increment(ref nCompleted);
                if (rec.OnFrameProcessed != null)
                    rec.OnFrameProcessed(rec, new OnFrameProcessedArgs(frameToAlign, completed, rec.NumFrames, 0));
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
    }
}
