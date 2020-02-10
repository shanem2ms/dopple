using System;
using TcpLib;
using System.IO;
using Dopple;

namespace FaceServer
{
	/// <SUMMARY>
	/// EchoServiceProvider. Just replies messages received from the clients.
	/// </SUMMARY>
	public class FaceMeshService : TcpServiceProvider
	{
        public event EventHandler<OnLiveFrameArgs> OnLiveFrame;
        public event EventHandler<OnDataReceived> OnDataReceived;
        public event EventHandler<OnNewRecordingArgs> OnNewRecording;
        DateTime lastLiveFrameTime = DateTime.MinValue;
        const int FRAMEID = 104;
        const int RECORDINGID = 103;
        string rcdName = "recorded.str";

        public override object Clone()
		{
            FaceMeshService fs = new FaceMeshService();
            fs.OnLiveFrame = this.OnLiveFrame;
            fs.OnDataReceived = this.OnDataReceived;
            fs.OnNewRecording = this.OnNewRecording;
            return fs;
		}

		public override void OnAcceptConnection(ConnectionState state)
		{
		}

        class Message
        {
            public Message(int messageId, long byteCount)
            {
                this.messageId = messageId;
                this.bytes = new byte[byteCount + 4];
            }
            public byte[] bytes;
            public int bytesRead = 0;
            public int messageId;
            public int DataLength {  get { return bytes.Length - 4; } }
            public int BytesRemaining {  get { return bytes.Length - bytesRead;  } }
        }


        void OnFrame(Message msg)
        {
            bool timedOut = (DateTime.Now - this.lastLiveFrameTime).Seconds > 30;
            Frame frame = Frame.FromBytes(msg.bytes);
            OnLiveFrame(this, new OnLiveFrameArgs(frame, timedOut));
            this.lastLiveFrameTime = DateTime.Now;
        }

        void OnRecordedData(Message msg)
        {
            string docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = System.IO.Path.Combine(docpath, rcdName);
            if (File.Exists(path))
            {
                int idx = 0;
                while (File.Exists(path + idx))
                {
                    idx++;
                }
                File.Copy(path, path + idx);
            }

            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            fs.Write(msg.bytes, 0, msg.bytes.Length);
            fs.Close();
        }

        Message curMessage = null;
        UInt64 totalBytes = 0;
        bool doDataEventing = false;
        public override void OnReceiveData(ConnectionState state)
        {
            while (state.AvailableData > 0)
            {
                int availableData = state.AvailableData;
                byte[] data = new byte[availableData];
                {
                    int bytesRead = state.Read(data, 0, availableData);
                    if (bytesRead != availableData)
                        throw new Exception("bad data read");
                    totalBytes += (UInt64)bytesRead;
                }

                int currentReadOffset = 0;
                while (currentReadOffset < availableData)
                {
                    if (curMessage == null)
                    {
                        if ((availableData - currentReadOffset) >= 8)
                        {
                            int message = BitConverter.ToInt32(data, currentReadOffset);
                            currentReadOffset += sizeof(Int32);
                            if (data.Length - currentReadOffset < sizeof(Int64))
                                throw new Exception("bad data read");
                            long dataSize = BitConverter.ToInt64(data, currentReadOffset);
                            currentReadOffset += sizeof(long);
                            curMessage = new Message(message, dataSize);
                            doDataEventing = message == RECORDINGID;
                            if (doDataEventing)
                            {
                                OnDataReceived(this, new FaceServer.OnDataReceived(FaceServer.OnDataReceived.State.Start,
                                    dataSize, 0));
                            }
                        }
                        else
                            throw new Exception("bad data read");
                    }

                    if ((availableData - currentReadOffset) > 0)
                    {
                        if ((availableData - currentReadOffset) >= curMessage.BytesRemaining)
                        {
                            Buffer.BlockCopy(data, currentReadOffset, curMessage.bytes, curMessage.bytesRead, curMessage.BytesRemaining);
                            currentReadOffset += curMessage.BytesRemaining;
                            if (curMessage.messageId == FRAMEID)
                            {
                                OnFrame(curMessage);
                            }
                            else if (curMessage.messageId == RECORDINGID)
                            {
                                OnRecordedData(curMessage);
                            }
                            if (doDataEventing)
                            {
                                OnDataReceived(this, new FaceServer.OnDataReceived(FaceServer.OnDataReceived.State.Complete,
                                                            curMessage.bytes.Length, curMessage.bytesRead));
                            }
                            curMessage = null;
                        }
                        else
                        {
                            Buffer.BlockCopy(data, currentReadOffset, curMessage.bytes, curMessage.bytesRead, availableData - currentReadOffset);
                            curMessage.bytesRead += availableData - currentReadOffset;
                            currentReadOffset = availableData;
                            if (doDataEventing)
                            {
                                OnDataReceived(this, new FaceServer.OnDataReceived(FaceServer.OnDataReceived.State.Sending,
                                curMessage.bytes.Length, curMessage.bytesRead));
                            }
                        }
                    }
                }
            }
        }
		public override void OnDropConnection(ConnectionState state)
		{
        }
    }
    
    public class OnLiveFrameArgs : EventArgs
    {
        public OnLiveFrameArgs(Frame v, bool isNew)
        {
            this.frame = v;
            this.IsNewSession = isNew;
        }
        public Frame frame;
        public bool IsNewSession;
    }

    public class OnDataReceived : EventArgs
    {

        public enum State
        {
            Start,
            Sending,
            Complete
        }
        public OnDataReceived(State _state, Int64 _totalData, Int64 _dataSoFar)
        {
            this.state = _state;
            this.totalData = _totalData;
            this.dataSoFar = _dataSoFar;
        }
        
        public State state;
        public Int64 totalData;
        public Int64 dataSoFar;
    }

    public class OnNewRecordingArgs : EventArgs
    {
        public OnNewRecordingArgs(string _filename)
        {
            this.filename = _filename;
        }

        public string filename;
    }


}
