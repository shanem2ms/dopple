using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using OpenTK;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Planes;

namespace Dopple
{
    public struct V3Pt
    {
        public Vector3 pt;
        public Vector2 spt;

        public V3Pt(Vector3 _pt, Vector2 _spt)
        {
            pt = _pt;
            spt = _spt;
        }
    }

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

        public bool HasDepth { get { return this.depthBytesPerRow != 0; } }
        public int ImageWidth { get { return this.imageWidth; } }
        public int ImageHeight { get { return this.imageHeight; } }
        public int ImageBytesPerRow { get { return this.imageBytesPerRow; } }
        public int DepthWidth { get { return this.depthWidth >> App.Settings.DepthLod; } }
        public int DepthHeight { get { return this.depthHeight >> App.Settings.DepthLod; } }
        
        public static VideoFrame FromBytes(byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new TypeCaster();
            MemoryStream ms = new MemoryStream(bytes);
            return (VideoFrame)bf.Deserialize(ms);
        }

        [DllImport("ptslib.dll")]
        public static extern void DepthInvFillNAN(IntPtr pDepthBuffer, IntPtr pOut, int depthWidth, int depthHeight);

        [DllImport("ptslib.dll")]
        public static extern float DepthEdge(IntPtr pDepthBuffer, IntPtr pOutEdges, int depthWidth, int depthHeight, int blur, int maxPts);

        [DllImport("ptslib.dll")]
        public static extern void DepthBlur(IntPtr pDepthBuffer, IntPtr pOutEdges, int depthWidth, int depthHeight, int blur);

        [DllImport("ptslib.dll")]
        public static extern void ImageBlur(IntPtr pImageBuffer, IntPtr pOutEdges, int imageWidth, int iamgeHeight, int blur);        

        [DllImport("ptslib.dll")]
        public static extern void DepthBuildLods(IntPtr pDepthBuffer, IntPtr outpts, int depthWidth, int depthHeight);

        [DllImport("ptslib.dll")]
        public static extern void DepthFindNormals(IntPtr pDepthPts, IntPtr pOutNormals, int ptx, int pty, int depthWidth, int depthHeight);

        [DllImport("ptslib.dll")]
        public static extern void SetPlaneConstants(float minDist, float splitThreshold, float minDPVal, float maxCoverage);

        public static void RefreshConstant()
        {
            SetPlaneConstants(App.Settings.PlaneMinSize, App.Settings.PlaneThreshold, App.Settings.MinDPVal, App.Settings.MaxCoverage);
        }

        [DllImport("ptslib.dll")]
        public static extern void DepthMakePlanes(IntPtr pDepthPts, IntPtr pOutVertices, IntPtr pOutTexCoords, int numVertices, out int vertexCnt,
            int depthWidth, int depthHeight);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy",
        CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        public float[]GetDepthVals()
        {
            float[] depthVals = new float[depthHeight * depthWidth];
            System.Buffer.BlockCopy(depthData, 0, depthVals,
                0, depthHeight * depthWidth * 4);
            if (App.Settings.DepthLod > 0)
                return GetDepthLods(depthVals, depthWidth, depthHeight)[App.Settings.DepthLod - 1];
            else
                return depthVals;
        }


        Vector3 ConvertDepthPt(int x, int y, float depthVal)
        {
            float ratioX = cameraCalibrationDims.X / DepthWidth;
            float ratioY = cameraCalibrationDims.Y / DepthHeight;
            Vector4 cMat = cameraCalibrationVals;
            float xScl = cMat.X / ratioX;
            float yScl = cMat.Y / ratioY;
            float xOff = cMat.Z / ratioX + 30;
            float yOff = cMat.W / ratioY;
            float xrw = (x - xOff) * depthVal / xScl;
            float yrw = (y - yOff) * depthVal / yScl;
            Vector4 viewPos = new Vector4(xrw, yrw, depthVal, 1);
            Matrix4 matTransform = Matrix4.CreateRotationZ(-(float)Math.PI / 2.0f) *
                Matrix4.CreateRotationY((float)Math.PI);
            Vector4 modelPos = viewPos * matTransform;
            return new Vector3(modelPos);
        }

        public Matrix4 CameraMatrix
        {
            get
            {
                float ratioX = cameraCalibrationDims.X / DepthWidth;
                float ratioY = cameraCalibrationDims.Y / DepthHeight;
                Vector4 cMat = cameraCalibrationVals;
                float xScl = ratioX / cMat.X;
                float yScl = ratioY / cMat.Y;
                float xOff = cMat.Z / ratioX + (30 * DepthWidth / 640);
                float yOff = cMat.W / ratioY;
                Matrix4 proj = new Matrix4(
                    new Vector4(xScl, 0, 0, -xOff * xScl),
                    new Vector4(0, yScl, 0, -yOff * yScl),
                    new Vector4(0, 0, 0, 1),
                    new Vector4(0, 0, 1, 0));

                return Matrix4.CreateRotationZ(-(float)Math.PI / 2.0f) *
                    Matrix4.CreateRotationY((float)Math.PI) *
                    proj;
            }
        }


        public Matrix4 ProjectionMat
        {
            get
            {
                return CameraMatrix.Inverted() *
                    Matrix4.CreateScale(-1.0f / DepthWidth, -1.0f / DepthHeight, 1) *
                    Matrix4.CreateTranslation(0.5f, 0.5f, 0);

            }
        }

        public Dictionary<int, V3Pt> CalcDepthPoints()
        {
            int depthHeight = DepthHeight;
            int depthWidth = DepthWidth;

            Dictionary<int, V3Pt> pos = new Dictionary<int, V3Pt>();

            float []depthVals = GetDepthVals();
            Matrix4 cm = CameraMatrix;
            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    float depthVal = depthVals[y * depthWidth + x];
                    if (!float.IsNaN(depthVal))
                    {
                        float z = 1 / depthVal;
                        Vector4 modelPos = Vector4.Transform(cm, new Vector4(x, y, z, 1));
                        modelPos /= modelPos.W;
                        pos.Add(y * depthWidth + x, new V3Pt(new Vector3(modelPos.X, modelPos.Y, modelPos.Z),
                            new Vector2((float)x / (float)(depthWidth - 1), (float)y / (float)(depthHeight - 1))));
                    }
                    else
                    {
                    }
                }
            }
            return pos;
        }


        public static float []GetDepthInv(float[] depthVals, int width, int height, out float avgVal)
        {
            IntPtr depthPtr = Marshal.AllocHGlobal(width * height * 4);
            IntPtr depthOut = Marshal.AllocHGlobal(width * height * 4);
            Marshal.Copy(depthVals, 0, depthPtr, depthVals.Length);
            avgVal = 0;
            DepthInvFillNAN(depthPtr, depthOut, width, height);

            float[] outVals = new float[width * height];
            Marshal.Copy(depthOut, outVals, 0, outVals.Length);
            Marshal.FreeHGlobal(depthPtr);
            Marshal.FreeHGlobal(depthOut);
            return outVals;
        }


        public float MaxEdge { get; set; }

        static float [][]GetDepthLods(float []depthVals, int width, int height)
        {
            int dw = width;
            int dh = height;

            int totalFloats = 0;
            int nLods = 0;
            while (dw >= 32)
            {
                dw /= 2;
                dh /= 2;

                totalFloats += (dw * dh);
                nLods++;
            }

            IntPtr depthPtr = Marshal.AllocHGlobal(width * height * 4);
            IntPtr depthOut = Marshal.AllocHGlobal(totalFloats * 4);
            Marshal.Copy(depthVals, 0, depthPtr, depthVals.Length);
            DepthBuildLods(depthPtr, depthOut, width, height);

            float[] alllods = new float[totalFloats];
            Marshal.Copy(depthOut, alllods, 0, totalFloats);

            Marshal.FreeHGlobal(depthPtr);
            Marshal.FreeHGlobal(depthOut);
            float[][] lods = new float[nLods][];

            dw = width;
            dh = height;

            int offset = 0;
            for (int i = 0; i < nLods; ++i)
            {
                dw /= 2;
                dh /= 2;

                lods[i] = new float[dw * dh];
                Array.Copy(alllods, offset, lods[i], 0, dw * dh);
                offset += dw * dh; 
            }

            return lods;
        }

        static float []GetBlurredDepth(float[] depthVals, int depthWidth, int depthHeight, int blur)
        {
            float[] depthBlur = null;
            int bytesPerFrame = depthWidth * depthHeight * 4;
            IntPtr depthPtr = Marshal.AllocHGlobal(bytesPerFrame);
            IntPtr depthOut = Marshal.AllocHGlobal(bytesPerFrame);
            Marshal.Copy(depthVals, 0, depthPtr, depthVals.Length);
            DepthBlur(depthPtr, depthOut, depthWidth, depthHeight, blur);
            depthBlur = new float[depthWidth * depthHeight];
            Marshal.Copy(depthOut, depthBlur, 0, depthBlur.Length);
            Marshal.FreeHGlobal(depthPtr);
            Marshal.FreeHGlobal(depthOut);
            return depthBlur;
        }

        public static byte[] GetBlurredImage(byte[] imageVals, int imageWidth, int imageHeight, int blur)
        {
            byte[] imageBlur = null;
            int bytesPerFrame = imageWidth * imageHeight;
            IntPtr imagePtr = Marshal.AllocHGlobal(bytesPerFrame);
            IntPtr imageOut = Marshal.AllocHGlobal(bytesPerFrame);
            Marshal.Copy(imageVals, 0, imagePtr, imageVals.Length);
            ImageBlur(imagePtr, imageOut, imageWidth, imageHeight, blur);
            imageBlur = new byte[imageVals.Length];
            Marshal.Copy(imageOut, imageBlur, 0, imageBlur.Length);
            Marshal.FreeHGlobal(imagePtr);
            Marshal.FreeHGlobal(imageOut);
            return imageBlur;
        }

        void CalcDepthEdges()
        {

            float[] dv = GetDepthVals();
            float []depthDiff = new float[DepthWidth * DepthHeight];
            //List<float> svals = new List<float>();

            for (int h = 1; h < (DepthHeight - 1); ++h)
            {
                for (int w = 1; w < (DepthWidth - 1); ++w)
                {
                    float dw00 = 1 / dv[h * DepthWidth + (w - 1)];
                    float dw01 = 1 / dv[h * DepthWidth + (w + 1)];
                    float dw10 = 1 / dv[(h - 1) * DepthWidth + w];
                    float dw11 = 1 / dv[(h + 1) * DepthWidth + w];
                    float dv0 = 1 / dv[h * DepthWidth + w];
                    float dw = Math.Abs((dv0 * 4) - (dw00 + dw01 + dw10 + dw11));
                    if (!float.IsNaN(dw) && !float.IsInfinity(dw))
                    {
                        MaxEdge = Math.Max(MaxEdge, dw);
                        //svals.Add(dw);
                    }
                    depthDiff[h * DepthWidth + w] = dw;
                }
            }

            //svals.Sort();
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
            int bytesPerFrame = DepthWidth * DepthHeight * 4 * 3;
            Vector3 []depthQuads = new Vector3[DepthWidth * DepthHeight * 6];
            IntPtr depthPtsPtr = Marshal.AllocHGlobal(bytesPerFrame);
            IntPtr depthNrmPtr = Marshal.AllocHGlobal(bytesPerFrame);
            IntPtr genVerticesPtr = Marshal.AllocHGlobal(depthQuads.Length * 12);
            IntPtr genTexCoordsPtr = Marshal.AllocHGlobal(depthQuads.Length * 12);

            float[] fpts = new float[this.DepthWidth * this.DepthHeight * 3];
            for (int i = 0; i < fpts.Length; ++i)
            {
                fpts[i] = float.PositiveInfinity;
            }

            var pts = CalcDepthPoints();
            foreach (var kv in pts)
            {
                int idx = kv.Key;
                fpts[idx * 3] = kv.Value.pt.X;
                fpts[idx * 3 + 1] = kv.Value.pt.Y;
                fpts[idx * 3 + 2] = kv.Value.pt.Z;
            }
            Marshal.Copy(fpts, 0, depthPtsPtr, fpts.Length);
            DepthFindNormals(depthPtsPtr, depthNrmPtr, 0, 0, DepthWidth, DepthHeight);
            int genCount = 0;
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
                    return typeof(MotionPoint);
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
        public MotionPoint []motionPoints;

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

    [Serializable]
    public struct MotionPoint
    {
        public double X, Y, Z;
        public double rX, rY, rZ;
        public double qX, qY, qZ, qW;
        public double gX, gY, gZ;
        public double timeStamp;

        public MotionPoint(double x, double y, double z,
            double rx, double ry, double rz,
            double gx, double gy, double gz,
            double qx, double qy, double qz, double qw,
            double t)
        {
            X = x;
            Y = y;
            Z = z;
            rX = rx;
            rY = ry;
            rZ = rz;
            qX = qx;
            qY = qy;
            qZ = qz;
            qW = qw;
            gX = gx;
            gY = gy;
            gZ = gz;
            timeStamp = t;
        }

        public override string ToString() => $"X = {X}, Y = {Y}, Z = {Z}";
    }

    public class NrmPt
    {
        public Vector3 pt;
        public Vector3 nrm = Vector3.UnitZ;
        public Vector2 spt;
    }

}
