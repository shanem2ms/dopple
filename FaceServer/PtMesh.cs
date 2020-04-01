using System;
using System.Linq;
using OpenTK;
using System.Collections.Generic;
using System.IO;
using ZeroFormatter;
using MeshDecimator;

namespace Dopple
{
    public class Visual
    {
        public Vector3[] pos = null;
        public Vector3[] texcoord = null;
        public Vector3[] normal = null;
        public UInt32[] indices = null;
        public enum ShadingType
        {
            MeshColor,
            NormalColors,
            VertexColors
        }

        public Vector3 color;
        public ShadingType shadingType;
        public bool wireframe = false;
        public float opacity = 1.0f;
    }

    public class ActiveMesh
    {
        public PtMesh mesh;
        public string name;
        public Vector3 meshcolor;
        public Vector3 translation = new Vector3();
        public Quaternion rotation = new Quaternion(0, 0, 0);
        public Matrix4? alignmentTransform = null;
        public Matrix4? overrideTransform = null;
        public bool isdirty = true;
        public bool needrebuild = true;
        public bool visible = true;
        public int id;
        public float opacity = 1.0f;
        public bool wireframe = false;
        public List<SPlane> meshPlanes;
        public OctNode octTree;
        public Visual.ShadingType shadingType = Visual.ShadingType.MeshColor;

        public Visual[] visuals = null;
        public Matrix4 WorldTransform
        {
            get
            {
                return overrideTransform.HasValue ? overrideTransform.Value :
                    Matrix4.CreateFromQuaternion(rotation) * Matrix4.CreateTranslation(translation);
            }
        }
    }

    static public class Vec3Extensions
    {
        public static Vector3 Min(this Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Min(a.X, b.X),
                Math.Min(a.Y, b.Y),
                Math.Min(a.Z, b.Z));

        }
        public static Vector3 Max(this Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Max(a.X, b.X),
                Math.Max(a.Y, b.Y),
                Math.Max(a.Z, b.Z));

        }
    }

    [ZeroFormattable]
    public class PtMesh
    {
        [Index(0)]
        public virtual V3L[] pos { get; set; }
        [Index(1)]
        public virtual V3[] color { get; set; }
        [Index(2)]
        public virtual V3[] normal { get; set; }
        [Index(3)]
        public virtual UInt32[] indices { get; set; }

        [Index(4)]
        public virtual int nPoints { get; set; }
        [Index(5)]
        public virtual Bucket[] buckets { get; set; }
        [Index(6)]
        public virtual M4 worldMatrix { get; set; }
        [Index(7)]
        public virtual int depthWidth { get; set; }
        [Index(8)]
        public virtual int depthHeight { get; set; }

        [ZeroFormattable]
        public struct V3
        {
            public V3(Vector3 v)
            {
                X = v.X;
                Y = v.Y;
                Z = v.Z;
            }
            public V3(float x, float y, float z)
            { X = x; Y = y; Z = z; }
            [Index(0)]
            public float X;
            [Index(1)]
            public float Y;
            [Index(2)]
            public float Z;
            [IgnoreFormat]
            public Vector3 Gl { get { return new Vector3(X, Y, Z); } }
        }

        [ZeroFormattable]
        public struct V3L
        {
            public V3L(Vector3 v, uint col, uint row)
            {
                X = v.X;
                Y = v.Y;
                Z = v.Z;
                Col = col;
                Row = row;
            }
            public V3L(float x, float y, float z, uint col, uint row)
            { X = x; Y = y; Z = z; Col = col; Row = row; }
            [Index(0)]
            public float X;
            [Index(1)]
            public float Y;
            [Index(2)]
            public float Z;
            [Index(3)]
            public uint Col;
            [Index(4)]
            public uint Row;
            [IgnoreFormat]
            public Vector3 Gl { get { return new Vector3(X, Y, Z); } }
        }

        [ZeroFormattable]
        public struct M4
        {
            public M4(float[] v)
            {
                vals = v;
            }

            public M4(Matrix4 m)
            {
                vals = new float[]
                {
                    m.M11, m.M12, m.M13, m.M14,
                    m.M21, m.M22, m.M23, m.M24,
                    m.M31, m.M32, m.M33, m.M34,
                    m.M41, m.M42, m.M43, m.M44
                };
            }

            [IgnoreFormat]
            public Matrix4 Gl { get
                {
                    if (vals == null)
                        return Matrix4.Identity;
                    return new Matrix4(vals[0], vals[1], vals[2], vals[3],
                        vals[4], vals[5], vals[6], vals[7],
                        vals[8], vals[9], vals[10], vals[11],
                        vals[12], vals[13], vals[14], vals[15]);
                } }

            [Index(0)]
            public float[] vals;
        }

        struct Vec3
        {
            public Vec3(float x, float y, float z)
            { X = x; Y = y; Z = z; }
            public float X;
            public float Y;
            public float Z;
        }

        static byte[] Vector3ToByte(Vector3[] vecs)
        {
            byte[] data = new byte[vecs.Length * sizeof(float) * 3];
            uint offset = 0;
            foreach (Vector3 v in vecs)
            {
                byte[] fdata = BitConverter.GetBytes(v.X);
                Array.Copy(fdata, 0, data, offset, sizeof(float));
                offset += sizeof(float);
                fdata = BitConverter.GetBytes(v.Y);
                Array.Copy(fdata, 0, data, offset, sizeof(float));
                offset += sizeof(float);
                fdata = BitConverter.GetBytes(v.Z);
                Array.Copy(fdata, 0, data, offset, sizeof(float));
                offset += sizeof(float);
            }

            return data;
        }

        static Vector3[] ByteToVector3(MemoryStream ms, int nPoints)
        {
            byte[] data = new byte[nPoints * sizeof(float) * 3];
            Vector3[] vecs = new Vector3[nPoints];
            ms.Read(data, 0, data.Length);
            int offset = 0;
            for (int i = 0; i < nPoints; ++i)
            {
                vecs[i] = new Vector3();
                vecs[i].X = BitConverter.ToSingle(data, offset);
                offset += sizeof(float);
                vecs[i].Y = BitConverter.ToSingle(data, offset);
                offset += sizeof(float);
                vecs[i].Z = BitConverter.ToSingle(data, offset);
                offset += sizeof(float);
            }

            return vecs;
        }

        public void WriteAsciiPts(System.IO.Stream stream)
        {
            StreamWriter sw = new StreamWriter(stream);
            for (int i = 0; i < this.pos.Length; ++i)
            {
                sw.WriteLine($"{pos[i].X} {pos[i].Y} {pos[i].Z} {normal[i].X} {normal[i].Y} {normal[i].Z}");
            }

            sw.Flush();
        }
        public byte[] ToBytes()
        {
            return ZeroFormatter.ZeroFormatterSerializer.Serialize<PtMesh>(this);
        }

        public static PtMesh FromBytes(byte[] bytes)
        {
            return ZeroFormatter.ZeroFormatterSerializer.Deserialize<PtMesh>(bytes);
        }

        static void CalcDiff(float[] vals, int begin, int end, int stride, int thresh,
            int buffer)
        {
            float lastVal = 0;
            for (int o = begin; o != end; o += stride)
            {
                float val = vals[o];
                vals[o] = vals[o] - lastVal;
                if (!float.IsNaN(val))
                    lastVal = val;
            }
        }

        public void GetMinMax(out Vector3 min, out Vector3 max)
        {
            min = this.pos[0].Gl;
            max = min;
            foreach (V3L v3 in this.pos)
            {
                Vector3 v = v3.Gl;
                min = min.Min(v);
                max = max.Max(v);
            }
        }

        public Vector3 CalcCenter()
        {
            Vector3 min, max;
            GetMinMax(out min, out max);
            return (min + max) * 0.5f;
        }
        void CenterPoints()
        {
            Vector3 ctr = CalcCenter();
            for (int i = 0; i < this.pos.Length; i++)
            {
                Vector3 v = this.pos[i].Gl;
                this.pos[i] = new V3L(v - ctr, this.pos[i].Col, this.pos[i].Row);
            }

            this.worldMatrix = new M4(Matrix4.CreateTranslation(ctr));
        }

        public static PtMesh CreateFromFrame(VideoFrame vf, Settings settings, Matrix4? faceCull)
        {
            if (!vf.HasDepth)
                return null;

            PtMesh mesh = new PtMesh();
            int depthHeight = vf.DepthHeight;
            int depthWidth = vf.DepthWidth;

            float[] vals = new float[depthHeight * depthWidth];
            float[] valsdx = new float[depthHeight * depthWidth];
            float[] valsdy = new float[depthHeight * depthWidth];


            System.Buffer.BlockCopy(vf.depthData, 0, vals,
                0, depthHeight * depthWidth * 4);
            Array.Copy(vals, valsdx, vals.Length);
            Array.Copy(vals, valsdy, vals.Length);

            float ratioX = vf.cameraCalibrationDims.X / depthWidth;
            float ratioY = vf.cameraCalibrationDims.Y / depthHeight;
            Vector4 cMat = vf.cameraCalibrationVals;
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

            List<V3L> pos = new List<V3L>();
            List<Vector3> col = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();

            int buffer = 10;

            for (int y = 0; y < depthHeight; ++y)
            {
                CalcDiff(valsdx, y * depthWidth, (y + 1) * depthWidth, 1, 1, buffer);
            }

            for (int x = 0; x < depthWidth; ++x)
            {
                CalcDiff(valsdy, x, depthHeight * depthWidth + x, depthWidth, 1, buffer);
            }

            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    int dx = (int)(((x - depthWidth / 2) + dOffsetX) * dSclX) +
                        depthWidth / 2;
                    int dy = (int)(((y - depthHeight / 2) + dOffsetY) * dSclY) +
                        depthHeight / 2;


                    float depthVal = float.NaN;
                    if (dx >= 0 && dx < depthWidth &&
                        dy >= 0 && dy < depthHeight)
                        depthVal = vals[dy * depthWidth + dx];
                    if (!float.IsNaN(depthVal) && depthVal < 1)
                    {
                        float xrw = (x - xOff) * depthVal / xScl;
                        float yrw = (y - yOff) * depthVal / yScl;
                        Vector4 viewPos = new Vector4(xrw, yrw, depthVal, 1);
                        Vector4 modelPos = viewPos * matTransform;

                        bool cull = false;
                        if (faceCull.HasValue)
                        {
                            Vector3 facesPos = Vector3.TransformPosition(new Vector3(modelPos), faceCull.Value);
                            cull = facesPos.X < 0 || facesPos.X > 1 ||
                                facesPos.Y < 0 || facesPos.Y > 1 ||
                                facesPos.Z < 0 || facesPos.Z > 1;
                        }
                        if (!cull)
                        {
                            float depthdx = valsdx[dy * depthWidth + dx];
                            float depthdy = valsdy[dy * depthWidth + dx];
                            Vector3 vector4Normal = new Vector3(depthdx / xScl * 10,
                                depthdy / yScl * 10, depthVal);
                            vector4Normal.Normalize();
                            Vector3 modelNrm = Vector3.TransformNormal(vector4Normal, matTransform);
                            pos.Add(new V3L(modelPos.X, modelPos.Y, modelPos.Z, (uint)x, (uint)y));
                            nrm.Add(modelNrm);
                            if (vf.imageData != null)
                                col.Add(vf.GetRGBVal(x * vf.ImageWidth / depthWidth, y *
                                    (vf.ImageHeight -1)/ depthHeight));
                            else
                                col.Add(new Vector3((float)x / depthWidth, (float)y / depthHeight, 0));
                        }
                    }
                }
            }

            mesh.depthWidth = depthWidth;
            mesh.depthHeight = depthHeight;
            if (pos.Count == 0)
                return null;

            mesh.pos = pos.ToArray();
            mesh.color = Array.ConvertAll(col.ToArray(), v => new V3(v));
            mesh.normal = Array.ConvertAll(nrm.ToArray(), v => new V3(v));
            mesh.CenterPoints();
            //mesh.CreateBuckets();
            //mesh.RemoveIslands(100);
            mesh.CreateIndices();
            return mesh;
        }
        
        class WeightedNormal
        {
            Vector3 total = new Vector3(0, 0, 0);
            uint cnt = 0;

            public void Add(Vector3 n)
            {
                total += n;
                cnt++;
            }

            public Vector3 GetNormal()
            {
                return cnt > 0 ? total.Normalized() :
                    new Vector3(0, 0, 0);
            }
        }
        void CreateIndices()
        {
            List<uint> indices = new List<uint>();

            int[] indicesMap = new int[this.depthWidth *
                this.depthHeight];
            for (int i = 0; i < indicesMap.Length; ++i)
                indicesMap[i] = -1;

            WeightedNormal[] wnormals = new WeightedNormal[this.pos.Length];
            for (int i = 0; i < wnormals.Length; ++i)
                wnormals[i] = new WeightedNormal();

            int idx = 0;
            foreach (V3L p in this.pos)
            {
                indicesMap[p.Row * this.depthWidth + p.Col] =
                    idx;
                idx++;
            }

            for (int y = 0; y < this.depthHeight - 1; ++y)
            {
                for (int x = 0; x < this.depthWidth - 1; ++x)
                {
                    int i0 = indicesMap[y * depthWidth + x];
                    int i1 = indicesMap[y * depthWidth + x + 1];
                    int i2 = indicesMap[(y + 1) * depthWidth + x];
                    int i3 = indicesMap[(y + 1) * depthWidth + x + 1];
                    if (i0 >= 0 && i1 >= 0 && i2 >= 0)
                    {
                        Vector3 pt0 = pos[i0].Gl;
                        Vector3 pt1 = pos[i1].Gl;
                        Vector3 pt2 = pos[i2].Gl;
                        if (IsValidTri(pt0, pt1, pt2))
                        {
                            indices.Add((uint)i0);
                            indices.Add((uint)i2);
                            indices.Add((uint)i1);
                            Vector3 trinrm = CalcNormal(pt0, pt2, pt1).Normalized();
                            wnormals[i0].Add(trinrm);
                            wnormals[i1].Add(trinrm);
                            wnormals[i2].Add(trinrm);
                        }
                    }
                    if (i1 >= 0 && i2 >= 0 && i3 >= 0)
                    {
                        Vector3 pt1 = pos[i1].Gl;
                        Vector3 pt2 = pos[i2].Gl;
                        Vector3 pt3 = pos[i3].Gl;
                        if (IsValidTri(pt1, pt2, pt3))
                        {
                            indices.Add((uint)i1);
                            indices.Add((uint)i2);
                            indices.Add((uint)i3);
                            Vector3 trinrm = CalcNormal(pt1, pt2, pt3).Normalized();
                            wnormals[i1].Add(trinrm);
                            wnormals[i2].Add(trinrm);
                            wnormals[i3].Add(trinrm);
                        }
                    }
                }
            }

            this.normal = Array.ConvertAll(wnormals, n => new V3(n.GetNormal()));
            this.indices = indices.ToArray();
        }

        void PerTriangleMode()
        {
            List<V3L> newvertices = new List<V3L>();
            List<V3> newcolors = new List<V3>();
            List<V3> newnormals = new List<V3>();
            List<uint> newindices = new List<uint>();

            for (int idx = 0; idx < indices.Length; idx += 3)
            {
                V3L pt0 = pos[this.indices[idx]];
                V3L pt1 = pos[this.indices[idx + 1]];
                V3L pt2 = pos[this.indices[idx + 2]];
                V3 trinrm = new V3(CalcNormal(pt0.Gl, pt2.Gl, pt1.Gl).Normalized());
                newvertices.Add(pt0);
                newvertices.Add(pt1);
                newvertices.Add(pt2);
                newnormals.Add(trinrm);
                newnormals.Add(trinrm);
                newnormals.Add(trinrm);
                newcolors.Add(this.color[this.indices[idx]]);
                newcolors.Add(this.color[this.indices[idx + 1]]);
                newcolors.Add(this.color[this.indices[idx + 2]]);
                newindices.Add((uint)idx);
                newindices.Add((uint)idx + 1);
                newindices.Add((uint)idx + 2);
            }

            this.pos = newvertices.ToArray();
            this.color = newcolors.ToArray();
            this.normal = newnormals.ToArray();
            this.indices = newindices.ToArray();
        }
        void CreateBuckets()
        {
            int maxPts = 1;
            uint curPt = 0;
            Dictionary<IPt, Bucket> bucketDict = new Dictionary<IPt, Bucket>();
            foreach (V3L p in pos)
            {
                IPt ipt = IPt.FromVec(p.Gl, 8);

                Bucket bucket;
                if (bucketDict.TryGetValue(ipt, out bucket))
                {
                    bucket.indices.Add(curPt);
                    maxPts = Math.Max(bucket.indices.Count, maxPts);
                }
                else
                {
                    bucket = new Bucket(ipt);
                    bucket.indices.Add(curPt);
                    bucketDict.Add(ipt, bucket);
                }

                curPt++;
            }

            this.buckets = bucketDict.Values.ToArray();
        }

        List<Vector3> GetVectorPerBucket()
        {
            List<Vector3> vectors = new List<Vector3>();
            foreach (Bucket bucket in this.buckets)
            {
                Vector3 vec = new Vector3();
                foreach (uint index in bucket.indices)
                {
                    vec += this.pos[index].Gl;
                }

                vec *= (1.0f / bucket.indices.Count());
                vectors.Add(vec);
            }
            return vectors;
        }

        public void ApplyMatrix(Matrix4 mat)
        {
            this.pos = this.pos.Select((p) => 
                new V3L(Vector3.TransformPosition(p.Gl, mat), p.Row, p.Col)).ToArray();
        }

        public void Combine(List<PtMesh> meshes)
        {
            int nPoints = 0;
            foreach (PtMesh mesh in meshes)
            {
                nPoints += mesh.pos.Length;
            }

            this.pos = new V3L[nPoints];
            this.color = new V3[nPoints];
            this.normal = new V3[nPoints];

            uint ptOffset = 0;
            Dictionary<IPt, Bucket> bucketDict = new Dictionary<IPt, Bucket>();
            foreach (PtMesh mesh in meshes)
            {
                uint nFrmPoints = (uint)mesh.pos.Length;
                Array.Copy(mesh.pos, 0, this.pos, ptOffset, nFrmPoints);
                Array.Copy(mesh.color, 0, this.color, ptOffset, nFrmPoints);
                Array.Copy(mesh.normal, 0, this.normal, ptOffset, nFrmPoints);

                foreach (Bucket inBucket in mesh.buckets)
                {
                    var offsetList = inBucket.indices.Select(v => v + ptOffset);
                    Bucket bucket;
                    if (bucketDict.TryGetValue(inBucket.ipt, out bucket))
                    {
                        bucket.indices.AddRange(offsetList);
                    }
                    else
                    {
                        bucketDict.Add(inBucket.ipt, new Bucket(inBucket.ipt));
                    }
                }
                ptOffset += nFrmPoints;
            }

            this.buckets = bucketDict.Values.ToArray();
        }

        bool IsValidTri(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            const float sqLen = 0.001f;
            if ((v1 - v2).LengthSquared > sqLen ||
                (v1 - v3).LengthSquared > sqLen ||
                (v2 - v3).LengthSquared > sqLen)
                return false;
            return true;
        }
        Vector3 CalcNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 nrm = Vector3.Cross((v2 - v1), (v3 - v2));
            return nrm.Normalized();
        }

        Dictionary<IPt, Bucket> GetBucketDict()
        {
            Dictionary<IPt, Bucket> dict = new Dictionary<IPt, Bucket>();
            foreach (Bucket bucket in buckets)
                dict.Add(bucket.ipt, bucket);
            return dict;
        }

        public void RemoveIslands(int minimumBlocks)
        {
            List<IPt> largestConnectedBlock = null;
            List<List<IPt>> connectedBlocks = new List<List<IPt>>();
            {
                Dictionary<IPt, Bucket> ptDict = GetBucketDict();
                while (ptDict.Count > 0)
                {
                    List<IPt> connectedBlock = new List<IPt>();
                    List<IPt> activeSearches = new List<IPt>();
                    IPt ptFirst = ptDict.First().Key;
                    ptDict.Remove(ptFirst);
                    activeSearches.Add(ptFirst);
                    connectedBlock.Add(ptFirst);

                    while (activeSearches.Count > 0)
                    {
                        List<IPt> newActiveSearches = new List<IPt>();

                        foreach (IPt iptSrch in activeSearches)
                        {
                            IPt[] ptCheckList = iptSrch.GetNeighbors();
                            foreach (IPt ptCheck in ptCheckList)
                            {
                                if (ptDict.ContainsKey(ptCheck))
                                {
                                    ptDict.Remove(ptCheck);
                                    newActiveSearches.Add(ptCheck);
                                    connectedBlock.Add(ptCheck);
                                }
                            }
                        }

                        activeSearches = newActiveSearches;
                    }

                    if (largestConnectedBlock == null ||
                        connectedBlock.Count > largestConnectedBlock.Count)
                        largestConnectedBlock = connectedBlock;
                    if (connectedBlock.Count > minimumBlocks)
                        connectedBlocks.Add(connectedBlock);
                }
            }
            List<V3L> newPts = new List<V3L>();
            List<V3> newCols = new List<V3>();
            List<V3> newNrm = new List<V3>();

            Dictionary<IPt, Bucket> oldBuckets = GetBucketDict();
            Dictionary<IPt, Bucket> newBuckets = new Dictionary<IPt, Bucket>();
            uint nextPt = 0;
            foreach (List<IPt> connectedBlock in connectedBlocks)
            {
                foreach (IPt pt in connectedBlock)
                {
                    Bucket newBucket = new Bucket(pt);
                    foreach (uint vec in oldBuckets[pt].indices)
                    {
                        newBucket.indices.Add(nextPt);
                        newPts.Add(this.pos[vec]);
                        newCols.Add(this.color[vec]);
                        newNrm.Add(this.normal[vec]);
                        nextPt++;
                    }
                    newBuckets.Add(pt, newBucket);
                }
            }
            this.pos = newPts.ToArray();
            this.color = newCols.ToArray();
            this.normal = newNrm.ToArray();
            this.buckets = newBuckets.Values.ToArray();
        }

        struct V2Idx : IComparable<V2Idx>
        {
            public V2Idx(uint i, float x, float y)
            { idx = i; X = x; Y = y; }
            public float X;
            public float Y;
            public uint idx;

            public int CompareTo(V2Idx other)
            {
                if (Y != other.Y)
                    return Y.CompareTo(other.Y);
                else
                    return X.CompareTo(other.X);
            }
        }

        public int GetCurLod()
        {
            return this.buckets != null ? this.buckets.First().ipt.Lod : -1;
        }
        public void ChangeBucketLod(int newlod)
        {
            this.buckets = GetBucketsForLod(newlod).ToArray();
        }

        public List<Bucket> GetBucketsForLod(int newlod)
        {
            List<Bucket> subBktList = new List<Bucket>();
            foreach (Bucket bucket in this.buckets)
            {
                GetBucketsForLod(bucket, newlod, subBktList);
            }

            return subBktList;
        }
        public void GetBucketsForLod(Bucket bucket, int newlod, List<Bucket> subBktList)
        {
            Dictionary<IPt, Bucket> subBuckets = new Dictionary<IPt, Bucket>();
            foreach (uint index in bucket.indices)
            {
                IPt ipt = IPt.FromVec(this.pos[index].Gl, newlod);
                Bucket nb;
                if (subBuckets.TryGetValue(ipt, out nb))
                {
                    nb.indices.Add(index);
                }
                else
                {
                    nb = new Bucket(ipt);
                    nb.indices.Add(index);
                    subBuckets.Add(ipt, nb);
                }
            }

            subBktList.AddRange(subBuckets.Values);
        }
        public void Init2()
        {
            foreach (Bucket bucket in this.buckets)
            {
                Vector3 nrmAvg = new Vector3();
                float totalCnt = 0;
                foreach (uint idx in bucket.indices)
                {
                    nrmAvg += this.normal[idx].Gl;
                    totalCnt += 1.0f;
                }

                nrmAvg /= totalCnt;
                nrmAvg.Normalize();
                foreach (uint idx in bucket.indices)
                {
                    this.normal[idx] = new V3(nrmAvg);
                }
            }
        }

        private static readonly Vector3[] _Cube = new Vector3[] {
            new Vector3(0.0f, 0.0f, 0.0f),  // 0 
            new Vector3(1.0f, 0.0f, 0.0f),  // 1
            new Vector3(1.0f, 1.0f, 0.0f),  // 2
            new Vector3(0.0f, 1.0f, 0.0f),  // 3
            new Vector3(0.0f, 0.0f, 1.0f),  // 4
            new Vector3(1.0f, 0.0f, 1.0f),  // 5
            new Vector3(1.0f, 1.0f, 1.0f),  // 6
            new Vector3(0.0f, 1.0f, 1.0f),  // 7
        };

        private static readonly uint[] _CubeIndices = new uint[]
        {
            0, 1, 2,
            0, 2, 3,
            4, 5, 6,
            4, 6, 7,

            0, 1, 5,
            0, 5, 4,
            2, 3, 7,
            2, 7, 6,

            0, 3, 7,
            0, 7, 4,
            1, 2, 6,
            1, 6, 5
        };

        public static void CubeMesh(Matrix4 transform, List<Vector3> pos, List<uint> indices)
        {
            uint offset = (uint)pos.Count;
            pos.AddRange(_Cube.Select((p) => Vector3.TransformPosition(p, transform)));
            indices.AddRange(_CubeIndices.Select((i) => i + offset));
        }

        public void Init()
        {

        }


        public Visual GetMeshVisual()
        {
            Visual v = new Visual();
            v.pos = pos.Select(p => new Vector3(p.X, p.Y, p.Z)).ToArray();
            v.normal = normal?.Select(p => new Vector3(p.X, p.Y, p.Z)).ToArray();
            v.texcoord = color?.Select(p => new Vector3(p.X, p.Y, p.Z)).ToArray();
            v.indices = indices.ToArray();
            return v;
        }

        public Visual GetDecimatedMeshVisual(int numTriangles)
        {
            var vertices = this.pos.Select(p => new MeshDecimator.Math.Vector3d(
                (double)p.X, (double)p.Y, (double)p.Z)).ToArray();
            Mesh m = new Mesh(vertices, this.indices.Select(i => (int)i).ToArray());
            m.Normals = this.normal.Select(p => new MeshDecimator.Math.Vector3(
                p.X, p.Y, p.Z)).ToArray();
            m.SetUVs(0, this.color.Select(p => new MeshDecimator.Math.Vector3(
                p.X, p.Y, p.Z)).ToArray());
            var algorithm = MeshDecimation.CreateAlgorithm(Algorithm.Default);
            Mesh destMesh = MeshDecimation.DecimateMesh(algorithm, m, numTriangles);

            List<Vector3> pos = destMesh.Vertices.Select(p => new Vector3((float)p.x, (float)p.y, (float)p.z)).ToList();
            List<Vector3> nrm = destMesh.Normals.Select(p => new Vector3(p.x, p.y, p.z)).ToList();
            List<uint> indices = destMesh.Indices.Select(i => (uint)i).ToList();
            int posCnt = pos.Count;
            /*
            for (int i = 0; i < posCnt; ++i)
            {
                GetNormalMesh(pos[i], nrm[i], 0.01f, pos, nrm, indices);
            }*/

            Visual v = new Visual();
            v.pos = pos.ToArray();
            v.normal = nrm.ToArray();
            v.indices = indices.ToArray();

            //List<MeshDecimator.Math.Vector3> uvs = new List<MeshDecimator.Math.Vector3>();
            //destMesh.GetUVs(0, uvs);
            //v.texcoord = uvs.Select(p => new Vector3(p.x, p.y, p.z)).ToArray();
            return v;
        }

        public void GetDecimatedMesh(int numTriangles, out Vector3 []pts, out Vector3 []nrm, out uint []indices)
        {
            var vertices = this.pos.Select(p => new MeshDecimator.Math.Vector3d(
                (double)p.X, (double)p.Y, (double)p.Z)).ToArray();
            Mesh m = new Mesh(vertices, this.indices.Select(i => (int)i).ToArray());
            m.Normals = this.normal.Select(p => new MeshDecimator.Math.Vector3(
                p.X, p.Y, p.Z)).ToArray();
            var algorithm = MeshDecimation.CreateAlgorithm(Algorithm.Default);
            Mesh destMesh = MeshDecimation.DecimateMesh(algorithm, m, numTriangles);
            pts = destMesh.Vertices.Select(p => new Vector3((float)p.x, (float)p.y, (float)p.z)).ToArray();
            indices = destMesh.Indices.Select(i => (uint)i).ToArray();
            nrm = destMesh.Normals.Select(p => new Vector3(p.x, p.y, p.z)).ToArray();
        }


        public OctNode GetTree(float maxdeviation)
        {
            Bucket nb = new Bucket();
            nb.indices = new List<uint>();
            for (uint i = 0; i < this.pos.Length; ++i)
                nb.indices.Add(i);
            nb.ipt = new IPt(0, 0, 0, 0);

            OctNode topNode = new OctNode(new IPt(0, 0, 0, 0), nb);
            topNode.PlanarBuild(maxdeviation,
                Array.ConvertAll(this.pos, p => p.Gl), Array.ConvertAll(this.normal, p => p.Gl));
            return topNode;
        }

        public List<SPlane> GetSPlanes(OctNode topNode)
        {
            List<SPlane> planes = new List<SPlane>();
            topNode.GetPlanes(planes);
            return planes;
        }

        public List<SPlane> GetPlanarVisual(OctNode topNode, out Visual vis, int itemIdx)
        {
            List<Vector3> ppos = new List<Vector3>();
            List<Vector3> pnrm = new List<Vector3>();
            List<Vector3> pcol = new List<Vector3>();
            List<uint> pind = new List<uint>();
            List<SPlane> planes = GetSPlanes(topNode);
            int planeIdx = 0;
            foreach (SPlane plane in planes)
            {
                plane.GetMeshUV(ppos, pnrm, pind);
                int planeb = itemIdx;
                int planeg = (planeIdx >> 8) & 0xFF;
                int planer = (planeIdx) & 0xFF;
                while (pcol.Count < ppos.Count)
                    pcol.Add(new Vector3((float)planer / 255.0f, (float)planeg / 255.0f, (float)planeb / 255.0f));
                planeIdx++;
            }

            vis = new Visual();
            vis.shadingType = Visual.ShadingType.MeshColor;
            vis.pos = ppos.ToArray();
            vis.texcoord = pcol.ToArray();
            vis.normal = pnrm.ToArray();
            vis.indices = pind.ToArray();
            return planes;
        }

        public void GetNormalsVisual(OctNode topNode, out Visual vis)
        {
            List<Vector3> ppos = new List<Vector3>();
            List<Vector3> pnrm = new List<Vector3>();
            List<uint> pind = new List<uint>();
            List<SPlane> planes = GetSPlanes(topNode);
            foreach (SPlane plane in planes)
            {
                GetNormalMesh(plane.ctr, plane.nrm, plane.size, ppos, pnrm, pind);
            }

            vis = new Visual();
            vis.shadingType = Visual.ShadingType.NormalColors;
            vis.pos = ppos.ToArray();
            vis.texcoord = null;
            vis.normal = pnrm.ToArray();
            vis.indices = pind.ToArray();
        }

        static public void GetNormalMesh(Vector3 pos, Vector3 nrm, float size, List<Vector3> outpos, List<Vector3> outnrm, List<uint> outind)
        {
            Vector3 cvec = (nrm.Y > nrm.Z) ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);
            Vector3 w = nrm.Normalized() * size;
            Vector3 u = Vector3.Cross(nrm, cvec).Normalized() * size * 0.025f;
            Vector3 v = Vector3.Cross(nrm, u).Normalized() * size * 0.025f;
            uint idx = (uint)outpos.Count;
            outpos.Add(pos - u - v);
            outpos.Add(pos + u + v);
            outpos.Add(pos + u + v + w);
            outpos.Add(pos - u - v + w);
            outnrm.Add(nrm);
            outnrm.Add(nrm);
            outnrm.Add(nrm);
            outnrm.Add(nrm);
            outind.Add(idx);
            outind.Add(idx + 1);
            outind.Add(idx + 2);
            outind.Add(idx);
            outind.Add(idx + 2);
            outind.Add(idx + 3);
        }

        public Visual GetRawCubeVisual(float pointscale)
        {
            List<uint> newindices = new List<uint>();
            List<Vector3> pts = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector3> col = new List<Vector3>();

            float len = pointscale;

            for (int posIdx = 0; posIdx < this.pos.Length; ++posIdx)
            {
                uint indexOffset = (uint)pts.Count;
                Vector3 min = this.pos[posIdx].Gl - new Vector3(len * 0.5f, len * 0.5f, len * 0.5f);
                for (int i = 0; i < 8; ++i)
                {
                    pts.Add(_Cube[i] * len + min);
                    nrm.Add(this.normal[i].Gl);
                    col.Add(this.color[posIdx].Gl);
                }
                foreach (uint index in _CubeIndices)
                {
                    newindices.Add(indexOffset + index);
                }
            }

            Visual vis = new Visual();
            vis.pos = pts.ToArray();
            vis.texcoord = col.ToArray();
            vis.normal = nrm.ToArray();
            vis.indices = newindices.ToArray();
            return vis;
        }

        static public Visual GetPointerVisual(Matrix4 mat)
        {
            Visual v = new Visual();
            List<uint> newindices = new List<uint>();
            List<Vector3> pts = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector3> col = new List<Vector3>();


            uint indexOffset = 0;
            for (int i = 0; i < 8; ++i)
            {
                pts.Add(Vector3.TransformPosition(_Cube[i], mat));
                nrm.Add(Vector3.TransformNormal(Vector3.UnitZ, mat));
                col.Add(new Vector3(1, 1, 1));
            }
            foreach (uint index in _CubeIndices)
            {
                newindices.Add(indexOffset + index);
            }

            Visual vis = new Visual();
            vis.pos = pts.ToArray();
            vis.texcoord = col.ToArray();
            vis.normal = nrm.ToArray();
            vis.indices = newindices.ToArray();
            return vis;
        }

        public void GetCubeVisual(int lod, out Vector3[] outpos,
            out Vector3[] outcolor,
            out Vector3[] outnormal,
            out uint[] outindices)
        {
            IEnumerable<Bucket> bkts = this.buckets;
            if (lod != GetCurLod())
                bkts = GetBucketsForLod(lod);

            List<uint> newindices = new List<uint>();
            List<Vector3> pts = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector3> col = new List<Vector3>();

            foreach (Bucket bucket in bkts)
            {
                Vector3 nrmAvg = new Vector3();
                float totalCnt = 0;
                foreach (uint idx in bucket.indices)
                {
                    nrmAvg += this.normal[idx].Gl;
                    totalCnt += 1.0f;
                }

                nrmAvg /= totalCnt;
                nrmAvg.Normalize();

                Vector3 min, max;
                bucket.ipt.GetCellBounds(out min, out max);
                Vector3 ext = max - min;

                uint indexOffset = (uint)pts.Count;
                for (int i = 0; i < 8; ++i)
                {
                    pts.Add(_Cube[i] * ext + min);
                    nrm.Add(nrmAvg);
                    col.Add(new Vector3(1, 1, 1));
                }
                foreach (uint index in _CubeIndices)
                {
                    newindices.Add(indexOffset + index);
                }
            }

            outpos = pts.ToArray();
            outcolor = col.ToArray();
            outnormal = nrm.ToArray();
            outindices = newindices.ToArray();
        }
    }
}