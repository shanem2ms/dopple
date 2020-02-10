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

        public bool HasDepth {  get { return this.DepthStride != 0; } }
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

        public static ARFrmHeader FromBytes(byte []bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new TypeCaster();
            MemoryStream ms = new MemoryStream(bytes);
            return (ARFrmHeader)bf.Deserialize(ms);
        }

        public PtMesh GetFaceMesh()
        {
            if (faceIndices == null)
                return null;
            PtMesh pm = new PtMesh();
            pm.pos = faceVertices.Select((p) => new PtMesh.V3L(Vector3.TransformPosition(p, worldMat), 0, 0)).ToArray();
            pm.normal = faceNormals != null ? faceNormals.Select((p) => new PtMesh.V3(Vector3.TransformNormal(p, worldMat))).ToArray() :
                null;
            pm.color = faceTextureCoords.Select((p) => new PtMesh.V3(p.X, p.Y, 0)).ToArray();
            pm.indices = faceIndices.Select((p) => (uint)p).ToArray();
            return pm;
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
        public PtMesh ptMesh;

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
            this.ptMesh = PtMesh.CreateFromFrame(this.vf, settings, faceCull);
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
