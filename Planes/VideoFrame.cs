using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using OpenTK;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace Dopple
{
    [Serializable]
    public class VideoFrame
    {
        public int depthWidth;
        public int depthHeight;
        public int depthBytesPerRow;
        public int imageWidth;
        public int imageHeight;
        public int imageBytesPerRow;

        public Matrix4 projectionMat;
        public Matrix4 viewMat;
        public Vector4 cameraCalibrationVals;
        public Vector2 cameraCalibrationDims;
        public byte[] depthData;
        public byte[] imageData;

        public bool HasDepth { get { return this.DepthStride != 0; } }
        public int ImageWidth { get { return this.imageWidth; } }
        public int ImageHeight { get { return this.imageHeight; } }
        public int ImageBytesPerRow { get { return this.imageBytesPerRow; } }
        public int DepthWidth { get { return this.depthWidth; } }
        public int DepthHeight { get { return this.depthHeight; } }
        public int DepthStride { get { return this.depthBytesPerRow; } }

        public static VideoFrame FromBytes(byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new TypeCaster();
            MemoryStream ms = new MemoryStream(bytes);
            return (VideoFrame)bf.Deserialize(ms);
        }

        public void GetDepthVals(out float minval, out float maxval, out float avgval)
        {
            minval = maxval = avgval = -1;
            if (DepthWidth == 0)
                return;
            float[] vals = new float[DepthHeight * DepthWidth];
            System.Buffer.BlockCopy(depthData, 0, vals,
                0, DepthHeight * DepthWidth * 4);
            bool isinit = false;
            avgval = 0;
            float avgCt = 0;
            for (int y = 0; y < DepthHeight; ++y)
            {
                for (int x = 0; x < DepthWidth; ++x)
                {
                    float val = vals[y * DepthWidth + x];
                    if (!float.IsNaN(val) &&
                        val > 0)
                    {
                        if (!isinit)
                        {
                            maxval = val;
                            minval = val;
                            isinit = true;
                        }
                        maxval = Math.Max(maxval, val);
                        minval = Math.Min(minval, val);
                        avgval += val;
                        avgCt += 1.0f;
                    }
                }
            }
            avgval /= avgCt;
        }

        [DllImport("dpengine.dll")]
        public static extern void DepthFindEdges(IntPtr pDepthBuffer, int depthWidth, int depthHeight);
        public void Blur()
        {
            IntPtr depthBuffer = Marshal.AllocHGlobal(sizeof(float) * DepthHeight * DepthWidth);
            Marshal.Copy(this.depthData, 0, depthBuffer, sizeof(float) * DepthHeight * DepthWidth);
            DepthFindEdges(depthBuffer, DepthWidth, DepthHeight);
            Marshal.Copy(depthBuffer, this.depthData, 0, sizeof(float) * DepthHeight * DepthWidth);
            Marshal.FreeHGlobal(depthBuffer);
        }

        [DllImport("dpengine.dll")]
        public static extern void ImageFindEdges(IntPtr pVideoBuffer, int videoWidth, int videoHeight);

        public void VidEdge()
        {
            IntPtr imageBuffer = Marshal.AllocHGlobal(this.imageData.Length);
            Marshal.Copy(this.imageData, 0, imageBuffer, this.imageData.Length);
            ImageFindEdges(imageBuffer, ImageWidth, imageHeight);
            Marshal.Copy(imageBuffer, this.imageData, 0, this.imageData.Length);
            Marshal.FreeHGlobal(imageBuffer);
        }

        [DllImport("ptslib.dll")]
        public static extern void DepthFindEdges(IntPtr pDepthBuffer, IntPtr pOutNormals, int depthWidth, int depthHeight);

        [DllImport("ptslib.dll")]
        public static extern void DepthFindNormals(IntPtr pDepthPts, IntPtr pOutNormals, int ptx, int pty, int depthWidth, int depthHeight);

        [DllImport("ptslib.dll")]
        public static extern void SetPlaneConstants(float minDist, float splitThreshold, float minDPVal);

        [DllImport("ptslib.dll")]
        public static extern void DepthMakePlanes(IntPtr pDepthPts, IntPtr pOutVertices, IntPtr pOutTexCoords, int numVertices, out int vertexCnt,
            int depthWidth, int depthHeight);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy",
        CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        public static float PlaneMinSize = 0.05f;
        public static float PlaneThreshold = 0.015f;
        public static float MinDPVal = 0.9f;

        public static void RefreshConstant()
        {
            SetPlaneConstants(PlaneMinSize, PlaneThreshold, MinDPVal);
        }

        private Vector3[] depthQuads = null;
        private Vector3[] depthTexColors = null;
        private Vector3[] depthNormals = null;
        private uint[] depthindices = null;
        float[] normvals = null;
        float[] depthCamPts = null;
        int genCount;


        public Vector3[] GetDepthPoints()
        {
            int depthHeight = DepthHeight;
            int depthWidth = DepthWidth;

            float[] vals = new float[depthHeight * depthWidth];
            float[] valsdx = new float[depthHeight * depthWidth];
            float[] valsdy = new float[depthHeight * depthWidth];


            System.Buffer.BlockCopy(depthData, 0, vals,
                0, depthHeight * depthWidth * 4);

            Array.Copy(vals, valsdx, vals.Length);
            Array.Copy(vals, valsdy, vals.Length);

            float ratioX = cameraCalibrationDims.X / depthWidth;
            float ratioY = cameraCalibrationDims.Y / depthHeight;
            Vector4 cMat = cameraCalibrationVals;
            float xScl = cMat.X / ratioX;
            float yScl = cMat.Y / ratioY;
            float xOff = cMat.Z / ratioX + 30;
            float yOff = cMat.W / ratioY;

            Matrix4 matTransform = Matrix4.CreateRotationZ(-(float)Math.PI / 2.0f) *
                Matrix4.CreateRotationY((float)Math.PI);

            int dOffsetX = 0;
            int dOffsetY = 0;
            float dSclX = 1.0f;
            float dSclY = 1.0f;

            List<Vector3> pos = new List<Vector3>();

            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    float depthVal = vals[y * depthWidth + x];
                    if (!float.IsNaN(depthVal))
                    {
                        float xrw = (x - xOff) * depthVal / xScl;
                        float yrw = (y - yOff) * depthVal / yScl;
                        Vector4 viewPos = new Vector4(xrw, yrw, depthVal, 1);
                        Vector4 modelPos = viewPos * matTransform;
                        pos.Add(new Vector3(modelPos.X, modelPos.Y, modelPos.Z));
                    }
                    else
                    {
                        pos.Add(new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
                    }
                }
            }
            return pos.ToArray();
        }
        void CopyToVector(float[] floats, Vector3[] vectors)
        {
            for (int vecIdx = 0; vecIdx < vectors.Length; ++vecIdx)
            {
                vectors[vecIdx].X = floats[vecIdx * 3];
                vectors[vecIdx].Y = floats[vecIdx * 3 + 1];
                vectors[vecIdx].Z = floats[vecIdx * 3 + 2];
            }
        }

        public void MakePlanes(out Vector3[] outPts, out Vector3[] outTex, out int cnt)
        {
            int bytesPerFrame = DepthStride * DepthHeight * 3;
            this.depthQuads = new Vector3[DepthWidth * DepthHeight * 6];
            IntPtr depthPtsPtr = Marshal.AllocHGlobal(bytesPerFrame);
            IntPtr depthNrmPtr = Marshal.AllocHGlobal(bytesPerFrame);
            IntPtr genVerticesPtr = Marshal.AllocHGlobal(depthQuads.Length * 12);
            IntPtr genTexCoordsPtr = Marshal.AllocHGlobal(depthQuads.Length * 12);

            Vector3[] pts = GetDepthPoints();
            int v3size = Marshal.SizeOf(pts[0]);
            float[] fpts = new float[pts.Length * 3];
            for (int idx = 0; idx < pts.Length; ++idx)
            {
                fpts[idx * 3] = pts[idx].X;
                fpts[idx * 3 + 1] = pts[idx].Y;
                fpts[idx * 3 + 2] = pts[idx].Z;
            }
            Marshal.Copy(fpts, 0, depthPtsPtr, fpts.Length);
            DepthFindNormals(depthPtsPtr, depthNrmPtr, 0, 0, DepthWidth, DepthHeight);
            DepthMakePlanes(depthPtsPtr, genVerticesPtr, genTexCoordsPtr, depthQuads.Length, out genCount,
                DepthWidth, DepthHeight);

            cnt = genCount;
            float[] outPtsf = new float[depthQuads.Length * 3];
            Marshal.Copy(genVerticesPtr, outPtsf, 0, outPtsf.Length);
            outPts = new Vector3[depthQuads.Length];
            CopyToVector(outPtsf, outPts);

            float[] outTexf = new float[depthQuads.Length * 3];
            outTex = new Vector3[depthQuads.Length];
            Marshal.Copy(genTexCoordsPtr, outTexf, 0, outTexf.Length);
            outTex = new Vector3[depthQuads.Length];
            CopyToVector(outTexf, outTex);

            Marshal.FreeHGlobal(depthPtsPtr);
            Marshal.FreeHGlobal(depthNrmPtr);
            Marshal.FreeHGlobal(genVerticesPtr);
            Marshal.FreeHGlobal(genTexCoordsPtr);

        }

        public Vector3 GetRGBVal(int ix, int iy)
        {
            int uvWidth = (this.ImageWidth);
            int uvOffset = (this.ImageWidth * this.ImageHeight);

            Matrix3 matyuv = new Matrix3(1, 1, 1,
                                    0, -0.18732f, 1.8556f,
                                    1.57481f, -0.46813f, 0);
            byte yVal = this.imageData[iy * ImageWidth + ix];
            int uvy = iy / 2;
            int uvx = ix & 0xFFFE;
            byte uVal = this.imageData[uvOffset + uvy * uvWidth + uvx];
            byte vVal = this.imageData[uvOffset + uvy * uvWidth + uvx + 1];
            Vector3 yuv = new Vector3(yVal / 255.0f, (uVal / 255.0f) - 0.5f, (vVal / 255.0f) - 0.5f);
            Vector3 rgb = yuv * matyuv;
            return new Vector3(rgb.X, rgb.Y, rgb.Z);
        }
    }

    [Serializable]
    public class ARFrmHeader
    {
        public Matrix4 projectionMat;
        public Matrix4 viewMat;
        public Matrix4 worldMat;
        public ushort[] faceIndices;
        public Vector3[] faceVertices;
        public Vector3[] faceNormals;
        public Vector2[] faceTextureCoords;

        public static ARFrmHeader FromBytes(byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new TypeCaster();
            MemoryStream ms = new MemoryStream(bytes);
            return (ARFrmHeader)bf.Deserialize(ms);
        }
    }

    public class TypeCaster : SerializationBinder
    {
        public TypeCaster()
        {
        }
        public override Type BindToType(string assemblyName, string typeName)
        {
            //typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
            switch (typeName)
            {
                case "Dopple.VideoFrame":
                    return typeof(VideoFrame);
                case "OpenTK.Vector3":
                    return typeof(OpenTK.Vector3);
                case "OpenTK.Vector2":
                    return typeof(OpenTK.Vector2);
                case "OpenTK.Vector4":
                    return typeof(OpenTK.Vector4);
                case "OpenTK.Matrix4":
                    return typeof(OpenTK.Matrix4);
                case "Dopple.ARFrmHeader":
                    return typeof(ARFrmHeader);
                case "OpenTK.Matrix3":
                    return typeof(OpenTK.Matrix3);
                case "Dopple.Frame":
                    return typeof(Frame);
                default:
                    throw new Exception();
            }
        }
    }


    [Serializable]
    public class Frame
    {
        public double timeStamp;
        public VideoFrame vf;
        public ARFrmHeader hdr;
        public double diffTime;
        public int idx;

        [NonSerialized]
        public Recording parentRecording = null;
        [NonSerialized]
        public Matrix4? AlignmentTransform = null;
        [NonSerialized]
        public int AlignmentToFrame = -1;
        [NonSerialized]
        public Matrix4? CumulativeAlignTrans = Matrix4.Identity;

        public void BuildData(Settings settings, Vector3? minbnd, Vector3? maxbnd)
        {
            AlignmentToFrame = -1;
            AlignmentTransform = null;
            Matrix4? faceCull = null;
            if (hdr != null)
            {
                Matrix4 cubmat =
                    Matrix4.CreateTranslation(-0.5f, -0.5f, -0.5f) *
                    Matrix4.CreateScale(0.3f, 0.3f, 0.3f) *
                    Matrix4.CreateTranslation(0, 0.05f, -0.02f) *
                    hdr.worldMat;
                cubmat.Invert();
                faceCull = cubmat;
            }
            else if (minbnd != null)
            {
                Matrix4 cubmat =
                    Matrix4.CreateTranslation(-0.5f, -0.5f, -0.5f) *
                    Matrix4.CreateScale(0.3f, 0.3f, 0.3f) *
                    Matrix4.CreateTranslation((maxbnd.Value + minbnd.Value) * 0.5f);
                cubmat.Invert();
                faceCull = cubmat;
            }
        }
        public bool HasFaceData { get { return hdr != null && hdr.faceVertices != null; } }
        public static Frame FromBytes(byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new TypeCaster();
            MemoryStream ms = new MemoryStream(bytes);
            return (Frame)bf.Deserialize(ms);
        }
    }
}
