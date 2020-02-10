using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using CoreFoundation;
using Foundation;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Dopple
{
    class DataTransmit
    {
        CFReadStream readStream;
        CFWriteStream writeStream;
        bool isRecording = false;
        bool liveTransmit = false;
        bool sendOnlyDepthFrames = true;
        string hostStr;
        string filepath;
        string currentRecoringName;
        public DataTransmit()
        {
            hostStr = NSUserDefaults.StandardUserDefaults.StringForKey(
                SettingsViewController.hostServerStr);
            if (hostStr != null)
            {
                CFStream.CreatePairWithSocketToHost(hostStr, 15555, out this.readStream, out this.writeStream);
                this.readStream.Open();
                this.writeStream.Open();
            }

            filepath =
                 Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public bool IsRecording { get { return isRecording; } set { isRecording = value; SetRecording(); } }
        public bool LiveTransmit { get { return liveTransmit; } set { liveTransmit = value; if (liveTransmit) SetLiveMode(); } }
        FileStream fileStream = null;
        Dictionary<double, Frame> currentFrames = new Dictionary<double, Frame>();

        public void AddARFrmHeader(double timeStamp, ARFrmHeader hdr)
        {
            lock (currentFrames)
            {
                Frame frame;
                if (currentFrames.TryGetValue(timeStamp, out frame))
                {
                    frame.hdr = hdr;
                }
                else
                {
                    frame = new Frame();
                    frame.timeStamp = timeStamp;
                    frame.hdr = hdr;
                    currentFrames.Add(timeStamp, frame);
                }
            }
        }

        public void AddVideoFrame(double timeStamp, VideoFrame vf)
        {
            lock (currentFrames)
            {
                Frame frame;
                if (currentFrames.TryGetValue(timeStamp, out frame))
                {
                    frame.vf = vf;
                }
                else
                {
                    frame = new Frame();
                    frame.timeStamp = timeStamp;
                    frame.vf = vf;
                    currentFrames.Add(timeStamp, frame);
                }
            }
            SendFrames(timeStamp);
        }

        double lastSend = 0;
        void SendFrames(double curTimeStamp)
        {
            double queueTime = 3.0f;
            double sendLatency = 0.5f;
            List<Frame> frames;
            lock (currentFrames)
            { frames = this.currentFrames.Values.ToList(); }
            foreach (Frame frame in frames)
            {
                if (//frame.IsComplete ||
                    (curTimeStamp - frame.timeStamp) > sendLatency)
                {
                    if (frame.HasDepth || !sendOnlyDepthFrames) SendMessage(104, frame);
                    this.currentFrames.Remove(frame.timeStamp);
                }
                else if ((curTimeStamp - frame.timeStamp) > queueTime)
                {
                    this.currentFrames.Remove(frame.timeStamp);
                }
            }

            lastSend = curTimeStamp;
        }

        public void SendMessage(Int32 messageType, object msgObj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            MemoryStream messageData = new MemoryStream();
            bf.Serialize(messageData, msgObj);
            Int64 datalen = messageData.Length;
            byte[] bytes = new byte[datalen + 16];
            Buffer.BlockCopy(BitConverter.GetBytes(messageType), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(datalen), 0, bytes, 4, 4);
            messageData.Seek(0, SeekOrigin.Begin);
            messageData.Read(bytes, 12, (int)datalen);
            UInt32 endMarker = 0xABCDEF12;
            Buffer.BlockCopy(BitConverter.GetBytes(endMarker), 0, bytes,
                (int)datalen + 12, 4);

            if (this.fileStream != null && this.isRecording)
            {
                this.fileStream.WriteAsync(bytes, 0, bytes.Length);
            }

            if (writeStream == null || !this.liveTransmit)
                return;
            if (this.sendThread == null)
            {
                this.sendThread = new Thread(SendDataThreadFunc);
                this.sendThread.Start();
            }

            lock (writeStream)
            {
                if (writeStream.GetStatus() == CFStreamStatus.Open)
                {

                    lock (this.queuedMessages)
                    {
                        this.queuedMessages.Add(new Message(bytes));
                        this.newMessageEvent.Set();
                    }
                }
            }
        }

        class Message
        {
            public Message(byte[] d)
            { this.data = d; }
            public byte[] data;
        }

        List<Message> queuedMessages = new List<Message>();
        AutoResetEvent newMessageEvent = new AutoResetEvent(false);
        System.Threading.Thread sendThread = null;

        void SendDataThreadFunc()
        {
            while (true)
            {
                newMessageEvent.WaitOne();
                Message msg = null;
                lock (queuedMessages)
                {
                    if (queuedMessages.Count > 0)
                        msg = queuedMessages.Last();
                    queuedMessages.Clear();
                }
                if (msg != null)
                {
                    int bytesWritten = 0;
                    while (bytesWritten < msg.data.Length)
                    {
                        int retVal = writeStream.Write(msg.data, bytesWritten,
                            msg.data.Length - bytesWritten);
                        if (retVal > 0)
                            bytesWritten += retVal;
                        else
                            Console.WriteLine("retval " + retVal);
                    }
                }
            }
        }

        void SetLiveMode()
        {
            if (hostStr != null && 
                (this.writeStream == null || this.writeStream.GetStatus() != CFStreamStatus.Open))
            {
                CFStream.CreatePairWithSocketToHost(hostStr, 15555, out this.readStream, out this.writeStream);
                this.readStream.Open();
                this.writeStream.Open();
            }
        }

        void SetRecording()
        {
            if (isRecording)
            {
                DateTime dt = DateTime.Now;
                string filename = $"{dt.Month}_{dt.Day}_{dt.Hour}_{dt.Minute}_{dt.Second}.dat";
                this.currentRecoringName = Path.Combine(this.filepath, filename);

                fileStream = new FileStream(this.currentRecoringName, FileMode.Create, FileAccess.Write);
            }
            else
            {
                fileStream.Flush();
                fileStream.Close();
                fileStream = null;
            }
        }

        public void TransferRecording(string name)
        {
            FileStream fileStrm = new FileStream(name, FileMode.Open, FileAccess.Read);
            byte[] bytes = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(103), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt64)fileStrm.Length), 0, bytes, 4, 4);
            writeStream.Write(bytes, 0, bytes.Length);
            int bytesWritten = 0;
            byte[] sendBytes = new byte[1024 * 1024];
            while (bytesWritten < fileStrm.Length)
            {
                int bytesToRead = Math.Min((int)bytes.Length,
                    (int)fileStrm.Length - bytesWritten);
                fileStrm.Read(sendBytes, 0, bytesToRead);
                int retVal = writeStream.Write(sendBytes, 0,
                    bytesToRead);
                if (retVal > 0)
                    bytesWritten += retVal;
                else
                    Console.WriteLine("retval " + retVal);
            }
            UInt32 endMarker = 0xABCDEF12;
            bytes = BitConverter.GetBytes(endMarker);
            writeStream.Write(bytes, 0, bytes.Length);
        }
    }
}
