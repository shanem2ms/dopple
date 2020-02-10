using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;
using OpenTK;
using SceneKit;
using System.Runtime.InteropServices;

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
        public int ImageStride { get { return ((this.imageWidth - 1) / 64 + 1) * 64; } }
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

        public void SetBuffers(IntPtr depthBufferAddress, IntPtr imageBufferAddr)
        {
            if (depthBufferAddress != IntPtr.Zero)
            {
                this.depthData = new byte[
                    depthBytesPerRow * depthHeight];
                Marshal.Copy(depthBufferAddress, depthData, 0, depthBytesPerRow * depthHeight);
            }
            if (imageBufferAddr != IntPtr.Zero)
            {
                this.imageData = new byte[
                    imageBytesPerRow * imageHeight];
                Marshal.Copy(imageBufferAddr, imageData, 0, imageBytesPerRow * imageHeight);
            }
        }

        public void DownScaleYUV(int scale)
        {
            int startOffset = 64;

            int width = this.imageWidth / scale;
            int height = this.imageHeight / scale;
            int scaleAdjW = this.imageWidth / width;
            int scaleAdjH = this.imageHeight / height;
            int iStride = ((this.imageWidth - 1) / 64 + 1) * 64;

            byte[] src = this.imageData;
            byte[] ds = new byte[width * height + width * height / 2];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    ds[y * width + x] =
                        src[(y * scaleAdjH) * iStride + (x * scaleAdjW) + startOffset];
                }
            }
            int uvSrc = (iStride * this.imageHeight + startOffset);
            int uvDst = width * height;

            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width / 2; ++x)
                {
                    ds[uvDst + y * width + (x * 2)] =
                        src[uvSrc + (y * scaleAdjH) * iStride + (x * scaleAdjW * 2)];
                    ds[uvDst + y * width + (x * 2) + 1] =
                        src[uvSrc + (y * scaleAdjH) * iStride + (x * scaleAdjW * 2) + 1];
                }
            }


            this.imageWidth = width;
            this.imageHeight = height;
            this.imageData = ds;
        }

        public void GetDepthVals(out float minval, out float maxval, out float avgval)
        {
            if (DepthWidth == 0)
            {
                minval = maxval = avgval = -1;
                return;
            }
            float[] vals = new float[DepthHeight * DepthWidth];
            System.Buffer.BlockCopy(depthData, 0, vals,
                0, DepthHeight * DepthWidth * 4);
            maxval = vals[0];
            minval = vals[0];
            avgval = 0;
            float avgCt = 0;
            for (int y = 0; y < DepthHeight; ++y)
            {
                for (int x = 0; x < DepthWidth; ++x)
                {
                    float val = vals[y * DepthWidth + x];
                    if (!float.IsNaN(val))
                    {
                        maxval = Math.Max(maxval, val);
                        minval = Math.Min(minval, val);
                        avgval += val;
                        avgCt += 1.0f;
                    }
                }
            }
            avgval /= avgCt;
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
            OpenTK.Vector3 yuv = new OpenTK.Vector3(yVal / 255.0f, (uVal / 255.0f) - 0.5f, (vVal / 255.0f) - 0.5f);
            OpenTK.Vector3 rgb;
            Matrix3.Transform(ref matyuv, ref yuv, out rgb);
            return new Vector3(rgb.X, rgb.Y, rgb.Z);
        }

        [Serializable]
        public class PtMesh
        {
            public Vector3[] pos;
            public Vector3[] color;
            public Vector3[] normal;
            public UInt32[] indices;
        }

        public PtMesh GetPointLists(Matrix4 invViewWorldMat)
        {
            if (depthData == null)
                return null;
            PtMesh ptMesh = new PtMesh();
            float[] vals = new float[DepthHeight * DepthWidth];
            int[] indicesMap = new int[DepthHeight * DepthWidth];

            System.Buffer.BlockCopy(depthData, 0, vals,
                0, DepthHeight * DepthWidth * 4);

            float ratio = cameraCalibrationDims.X / DepthWidth;
            Vector4 cMat = this.cameraCalibrationVals;
            float xScl = cMat.X / ratio;
            float yScl = cMat.Y / ratio;
            float xOff = cMat.Z / ratio;
            float yOff = cMat.W / ratio;

            Matrix4 matTransform = invViewWorldMat * Matrix4.CreateRotationY(180) * Matrix4.CreateRotationZ(-90);

            List<Vector3> pos = new List<Vector3>();
            List<Vector3> col = new List<Vector3>();
            for (int y = 0; y < DepthHeight; ++y)
            {
                for (int x = 0; x < DepthWidth; ++x)
                {
                    float depthVal = vals[y * DepthWidth + x];
                    if (!float.IsNaN(depthVal))
                    {
                        float xrw = (x - xOff) * depthVal / xScl;
                        float yrw = (y - yOff) * depthVal / yScl;
                        OpenTK.Vector4 viewPos = new OpenTK.Vector4(xrw, yrw, depthVal, 1);
                        OpenTK.Vector4 worldPos = OpenTK.Vector4.Transform(viewPos, matTransform);
                        indicesMap[y * DepthWidth + x] = pos.Count;
                        pos.Add(new Vector3(worldPos.X, worldPos.Y, worldPos.Z));
                        if (this.imageData != null)
                            col.Add(GetRGBVal(x * ImageWidth / DepthWidth, y * ImageHeight / DepthHeight));
                        else
                            col.Add(new Vector3((float)x / DepthWidth, (float)y / DepthHeight, 0));
                    }
                    else
                    {
                        indicesMap[y * DepthWidth + x] = -1;
                    }
                }
            }

            ptMesh.pos = pos.ToArray();
            ptMesh.color = col.ToArray();
            ptMesh.normal = new Vector3[pos.Count];
            List<uint> indices = new List<uint>();

            for (int y = 0; y < DepthHeight - 1; ++y)
            {
                for (int x = 0; x < DepthWidth - 1; ++x)
                {
                    int i0 = indicesMap[y * DepthWidth + x];
                    int i1 = indicesMap[y * DepthWidth + x + 1];
                    int i2 = indicesMap[(y + 1) * DepthWidth + x];
                    int i3 = indicesMap[(y + 1) * DepthWidth + x + 1];
                    if (i0 >= 0 && i1 >= 0 && i2 >= 0)
                    {
                        Vector3 pt0 = ptMesh.pos[i0];
                        Vector3 pt1 = ptMesh.pos[i1];
                        Vector3 pt2 = ptMesh.pos[i2];
                        ptMesh.normal[i0] = CalcNormal(pt0, pt1, pt2); ;
                        ptMesh.normal[i0].Normalize();
                        if (IsValidTri(pt0, pt1, pt2))
                        {
                            indices.Add((uint)i0);
                            indices.Add((uint)i2);
                            indices.Add((uint)i1);
                        }
                    }
                    if (i1 >= 0 && i2 >= 0 && i3 >= 0)
                    {
                        Vector3 pt1 = ptMesh.pos[i1];
                        Vector3 pt2 = ptMesh.pos[i2];
                        Vector3 pt3 = ptMesh.pos[i3];
                        if (IsValidTri(pt1, pt2, pt3))
                        {
                            indices.Add((uint)i1);
                            indices.Add((uint)i2);
                            indices.Add((uint)i3);
                        }
                    }
                }
            }

            ptMesh.indices = indices.ToArray();
            return ptMesh;
        }

        float LengthSq(Vector3 v)
        {
            return v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        }
        bool IsValidTri(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            const float sqLen = 0.001f;
            if (LengthSq(v1 - v2) > sqLen ||
                LengthSq(v1 - v3) > sqLen ||
                LengthSq(v2 - v3) > sqLen)
                return false;
            return true;
        }
        Vector3 CalcNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            
            Vector3 nrm = Vector3.Cross((v2 - v1), (v3 - v2));

            return Vector3.Normalize(nrm);
        }
    }

    [Serializable]
    public class ARFrmHeader
    {
        public Matrix4 projectionMat;
        public Matrix4 viewMat;
        public Matrix4 worldMat;
        public ushort[] faceIndices;
        public OpenTK.Vector3[] faceVertices;
        public OpenTK.Vector3[] faceNormals;
        public OpenTK.Vector2[] faceTextureCoords;

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
            typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
            switch (typeName)
            {
                case "VideoFrame":
                    return typeof(VideoFrame);
                case "Vector3":
                    return typeof(Vector3);
                case "Vector2":
                    return typeof(Vector2);
                case "Vector4":
                    return typeof(Vector4);
                case "Matrix44f":
                    return typeof(Matrix4);
                case "ARFrmHeader":
                    return typeof(ARFrmHeader);
                case "Matrix33f":
                    return typeof(Matrix3);
                case "Frame":
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
        public VideoFrame.PtMesh ptMesh;
        public bool HasDepth
        { get { return vf != null && vf.HasDepth; } }

        public void BuildData()
        {
            if (!HasFaceData)
                return;

            Matrix4 viewMat = Matrix4.Mult(this.hdr.viewMat, this.hdr.worldMat);
            Matrix4 invViewWorldMat = viewMat;
            invViewWorldMat.Invert();
            this.ptMesh = vf.GetPointLists(invViewWorldMat);
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
