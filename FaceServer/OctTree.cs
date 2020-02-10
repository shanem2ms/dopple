using System;
using System.Collections.Generic;
using System.Linq;
using ZeroFormatter;
using OpenTK;

namespace Dopple
{
    [ZeroFormattable]
    public struct IPt : IComparable<IPt>
    {
        public IPt(int lod, uint x, uint y, uint z)
        { X = x; Y = y; Z = z; Lod = lod; }

        [Index(0)]
        public int Lod { get; set; }
        [Index(1)]
        public uint X { get; set; }
        [Index(2)]
        public uint Y { get; set; }
        [Index(3)]
        public uint Z { get; set; }

        static Vector3 minVal = new Vector3(-1, -1, -1);
        static float extents = 2;
        static float extentsInv = 1.0f / extents;

        public static ulong CellCount(int lod)
        {
            ulong cldm = (ulong)(1 << lod);
            return (ulong)(cldm * cldm * cldm);
        }

        public static float CellSize(int lod)
        {
            int cellCt = 1 << lod;
            return extents / (float)cellCt;
        }

        [IgnoreFormat]
        public ulong CellIndex
        {
            get
            {
                ulong cldm = (ulong)(1 << Lod);
                return (ulong)(cldm * cldm * X +
                    cldm * Y + Z);
            }
        }
        public static IPt FromVec(Vector3 vec, int lod)
        {
            float cellCt = 1 << lod;
            Vector3 nrmPt = (vec - minVal) * extentsInv;
            IPt ipt = new IPt(lod, (uint)(nrmPt.X * cellCt),
                        (uint)(nrmPt.Y * cellCt),
                        (uint)(nrmPt.Z * cellCt));
            return ipt;
        }

        public IPt GetParentAtLod(int lod)
        {
            if (lod >= this.Lod)
                return this;

            int levelShift = (this.Lod - lod);
            return new IPt(lod, X >> levelShift,
                Y >> levelShift,
                Z >> levelShift);
        }

        public IPt GetChild(int idx)
        {
            uint xInc = (uint)(idx & 1);
            uint yInc = (uint)((idx >> 1) & 1);
            uint zInc = (uint)((idx >> 2) & 1);
            return new IPt(Lod + 1, (X << 1) + xInc,
                (Y << 1) + yInc,
                (Z << 1) + zInc);
        }

        public int IndexOfChild(IPt child)
        {
            if (child.Lod <= Lod)
                return -1;

            IPt childLoc = child;

            if (child.Lod > (Lod + 1))
                childLoc = childLoc.GetParentAtLod(Lod + 1);

            uint localX = childLoc.X - (X << 1);
            uint localY = childLoc.Y - (Y << 1);
            uint localZ = childLoc.Z - (Z << 1);

            if (localX > 1 || localY > 1 || localZ > 1)
                return -1;

            return (int)((localZ << 2) | (localY << 1) | localX);
        }
        public void GetCellBounds(out Vector3 min, out Vector3 max)
        {
            int cellCt = 1 << this.Lod;
            float cellSize = extents / (float)cellCt;
            min = new Vector3(cellSize * X + minVal.X,
                cellSize * Y + minVal.Y,
                cellSize * Z + minVal.Z);
            max = new Vector3(cellSize * (X + 1) + minVal.X,
                cellSize * (Y + 1) + minVal.Y,
                cellSize * (Z + 1) + minVal.Z);
        }
        public Vector3 GetCellCenter()
        {
            int cellCt = 1 << this.Lod;
            float cellSize = extents / (float)cellCt;
            return new Vector3(cellSize * (X + 0.5f) + minVal.X,
                cellSize * (Y + 0.5f) + minVal.Y,
                cellSize * (Z + 0.5f) + minVal.Z);
        }

        public IPt[] GetNeighbors()
        {
            return new IPt[] {
                    new IPt(this.Lod, this.X - 1, this.Y, this.Z),
                    new IPt(this.Lod, this.X + 1, this.Y, this.Z),
                    new IPt(this.Lod, this.X, this.Y - 1, this.Z),
                    new IPt(this.Lod, this.X, this.Y + 1, this.Z),
                    new IPt(this.Lod, this.X, this.Y, this.Z - 1),
                    new IPt(this.Lod, this.X, this.Y, this.Z + 1) };
        }

        public ulong DistSqFromCenter()
        {
            long cellCtH = (long)CellCount(this.Lod) / 2;
            long dx = ((long)X - cellCtH);
            long dy = ((long)Y - cellCtH);
            long dz = ((long)Z - cellCtH);
            return (ulong)(dx * dx) + (ulong)(dy * dy) + (ulong)(dz + dz);
        }

        public ulong DistSqFrom(IPt other)
        {
            long dx = ((long)X - (long)other.X);
            long dy = ((long)Y - (long)other.Y);
            long dz = ((long)Z - (long)other.Z);
            return (ulong)(dx * dx) + (ulong)(dy * dy) + (ulong)(dz + dz);
        }

        public ulong DistSqFrom(List<IPt> others)
        {
            ulong totalDist = 1;
            foreach (IPt ipt in others)
            {
                totalDist *= DistSqFrom(ipt);
            }

            return totalDist;
        }

        public int CompareTo(IPt other)
        {
            if (Lod != other.Lod)
                return Lod.CompareTo(other.Lod);
            if (X != other.X)
                return X.CompareTo(other.X);
            if (Y != other.Y)
                return Y.CompareTo(other.Y);
            if (Z != other.Z)
                return Z.CompareTo(other.Z);
            return 0;
        }


        public override string ToString()
        {
            return $"{Lod} [{X}, {Y}, {Z}]";
        }
    }


    [ZeroFormattable]
    public class Triangle
    {
        [Index(0)]
        public virtual uint[] indices { get; set; } = null;

        public Triangle()
        { indices = null; }
        public Triangle(uint i1, uint i2, uint i3)
        { indices = new uint[] { i1, i2, i3 }; }
    }

    public class SPlane
    {
        public Vector3 ctr;
        public Vector3 nrm;
        public Vector2 min;
        public Vector2 max;
        public float size;
        public OctNode node;

        public void GetMesh(List<Vector3> outpos, List<Vector3> outnrm, List<uint> outind)
        {
            Vector3 cvec = (this.nrm.Y > this.nrm.Z) ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);
            Vector3 u = Vector3.Cross(this.nrm, cvec).Normalized() * this.size;
            Vector3 v = Vector3.Cross(this.nrm, u).Normalized() * this.size;

            uint idx = (uint)outpos.Count;
            outpos.Add(this.ctr - u - v);
            outpos.Add(this.ctr - u + v);
            outpos.Add(this.ctr + u + v);
            outpos.Add(this.ctr + u - v);
            outnrm.Add(this.nrm);
            outnrm.Add(this.nrm);
            outnrm.Add(this.nrm);
            outnrm.Add(this.nrm);
            outind.Add(idx);
            outind.Add(idx + 1);
            outind.Add(idx + 2);
            outind.Add(idx);
            outind.Add(idx + 2);
            outind.Add(idx + 3);
        }

        public void GetMeshUV(List<Vector3> outpos, List<Vector3> outnrm, List<uint> outind)
        {
            Vector3 cvec = (this.nrm.Y > this.nrm.Z) ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);
            Vector3 u = Vector3.Cross(this.nrm, cvec).Normalized();
            Vector3 v = Vector3.Cross(this.nrm, u).Normalized();
            Vector3 minu = u * min.X;
            Vector3 minv = v * min.Y;
            Vector3 maxu = u * max.X;
            Vector3 maxv = v * max.Y;

            uint idx = (uint)outpos.Count;
            outpos.Add(this.ctr + minu + minv);
            outpos.Add(this.ctr + minu + maxv);
            outpos.Add(this.ctr + maxu + maxv);
            outpos.Add(this.ctr + maxu + minv);
            outnrm.Add(this.nrm);
            outnrm.Add(this.nrm);
            outnrm.Add(this.nrm);
            outnrm.Add(this.nrm);
            outind.Add(idx);
            outind.Add(idx + 1);
            outind.Add(idx + 2);
            outind.Add(idx);
            outind.Add(idx + 2);
            outind.Add(idx + 3);
        }

    }

    [ZeroFormattable]
    public class Bucket : IComparable<Bucket>
    {
        public Bucket()
        { }
        public Bucket(IPt _ipt)
        {
            ipt = _ipt;
        }
        [Index(0)]
        public virtual IPt ipt { get; set; }
        [Index(1)]
        public virtual List<uint> indices { get; set; } = new List<uint>();
        [Index(2)]
        public virtual List<Triangle> triangles { get; set; } = new List<Triangle>();

        public int CompareTo(Bucket other)
        {
            return ipt.CompareTo(other.ipt);
        }
    }

    [ZeroFormattable]
    public class OctNode
    {
        [Index(0)]
        public virtual IPt Loc { get; set; }
        [Index(1)]
        public virtual OctNode[] Children { get; set; } = null;
        [Index(2)]
        public virtual Bucket Bucket { get; set; }

        Vector3 avgNrm = new Vector3();
        Vector3 avgPos = new Vector3();
        Vector3 minPlanePt = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxPlanePt = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        public OctNode(IPt loc)
        {
            Loc = loc;
        }

        public OctNode(IPt loc, Bucket b)
        {
            Loc = loc;
            Bucket = b;
        }

        public void AddNode(OctNode node)
        {
            if (Loc.Lod >= node.Loc.Lod)
                throw new Exception("Too far");

            if (Children == null)
                Children = new OctNode[8];
            int childIdx = Loc.IndexOfChild(node.Loc);

            if (Loc.Lod + 1 == node.Loc.Lod)
            {
                Children[childIdx] = node;
            }
            else
            {
                if (Children[childIdx] == null)
                    Children[childIdx] = new OctNode(Loc.GetChild(childIdx));
                Children[childIdx].AddNode(node);
            }
        }

        private void CalcAverages(Vector3[] pos, Vector3[] nrm)
        {
            if (this.Children == null)
            {
                Vector3 anrm = new Vector3(0, 0, 0);
                Vector3 apos = new Vector3(0, 0, 0);
                foreach (uint ind in Bucket.indices)
                {
                    anrm += nrm[ind];
                    apos += pos[ind];
                }
                this.avgPos = apos * (1.0f / (float)Bucket.indices.Count);
                this.avgNrm = anrm.LengthSquared > 0 ? anrm.Normalized() :
                    new Vector3(0, 0, 0);
            }
            else
            {
                Vector3 anrm = new Vector3(0, 0, 0);
                Vector3 apos = new Vector3(0, 0, 0);
                int cnt = 0;
                foreach (OctNode node in this.Children)
                {
                    if (node != null)
                    {
                        node.CalcAverages(pos, nrm);
                        anrm += node.avgNrm;
                        apos += node.avgPos;
                        cnt++;
                    }
                }
                this.avgPos = apos * (1.0f / (float)cnt);
                this.avgNrm = anrm.LengthSquared > 0 ? anrm.Normalized() :
                    new Vector3(0, 0, 0);
            }
        }

        private void GetPlanarBounds(Vector3[] pos, Vector3 ctr, Vector3 nrm,
            out Vector3 min, out Vector3 max)
        {
            Vector3 cvec = (nrm.Y > nrm.Z) ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);
            Vector3 uVec = Vector3.Cross(nrm, cvec).Normalized();
            Vector3 vVec = Vector3.Cross(nrm, uVec).Normalized();
            Matrix3 mat = new Matrix3(uVec, vVec, nrm);

            Vector3 maxval = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 minval = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            if (this.Bucket != null)
            {
                foreach (uint ind in Bucket.indices)
                {
                    Vector3 planval = Vector3.Transform(mat, pos[ind] - ctr);
                    maxval = maxval.Max(planval);
                    minval = minval.Min(planval);
                }
            }

            min = minval;
            max = maxval;
        }

        public void GetPlanes(List<SPlane> planes)
        {
            if (Children == null)
            {
                SPlane plane = new SPlane();
                float ext = IPt.CellSize(this.Loc.Lod) * 0.5f;
                plane.ctr = this.avgPos;
                plane.min = new Vector2(minPlanePt.X, minPlanePt.Y);
                plane.max = new Vector2(maxPlanePt.X, maxPlanePt.Y);
                plane.nrm = this.avgNrm;
                plane.size = ext;
                plane.node = this;
                planes.Add(plane);
            }
            else if (this.Children != null)
            {
                foreach (OctNode node in this.Children)
                {
                    if (node != null)
                    {
                        node.GetPlanes(planes);
                    }
                }
            }
        }

        private void FillNormals(Vector3 avgNrmPar)
        {
            if (this.avgNrm.LengthSquared == 0)
                this.avgNrm = avgNrmPar;

            if (this.Children != null)
            {
                foreach (OctNode node in this.Children)
                {
                    if (node != null)
                    {
                        node.FillNormals(this.avgNrm);
                    }
                }
            }
        }
        public bool Simplify(Vector3[] pos, Vector3[] nrm)
        {
            CalcAverages(pos, nrm);
            FillNormals(this.avgNrm);
            return true;
        }

        public void PlanarBuild(float maxdeviation, Vector3[] pos, Vector3[] nrm)
        {
            avgNrm = Vector3.Zero;
            avgPos = Vector3.Zero;
            foreach (uint idx in this.Bucket.indices)
            {
                avgNrm += nrm[idx];
                avgPos += pos[idx];
            }
            avgNrm /= (float)this.Bucket.indices.Count;
            avgNrm.Normalize();
            avgPos /= (float)this.Bucket.indices.Count;

            float maxDev = 0;

            foreach (uint idx in this.Bucket.indices)
            {
                float deviation = Math.Abs(Vector3.Dot(pos[idx] - avgPos, avgNrm));
                maxDev = Math.Max(deviation, maxDev);
            }

            if (maxDev > maxdeviation)
            {
                this.Children = new OctNode[8];
                foreach (uint idx in this.Bucket.indices)
                {
                    IPt ipt = IPt.FromVec(pos[idx], this.Loc.Lod + 1);
                    int childIdx = this.Loc.IndexOfChild(ipt);
                    if (this.Children[childIdx] == null)
                    {
                        this.Children[childIdx] = new OctNode(ipt);
                        this.Children[childIdx].Bucket = new Bucket();
                    }
                    this.Children[childIdx].Bucket.indices.Add(idx);
                }

                this.Bucket = null;

                foreach (OctNode child in this.Children)
                {
                    if (child != null)
                        child.PlanarBuild(maxdeviation, pos, nrm);
                }
            }
            else
            {
                GetPlanarBounds(pos, this.avgPos, this.avgNrm, out this.minPlanePt, out this.maxPlanePt);
                //System.Diagnostics.Debug.WriteLine($"{this.Loc} - {this.Bucket.indices.Count}");
            }
        }

        public void GetBucketsForLod(List<Bucket> outBuckets, int lod)
        {
            if (this.Loc.Lod < lod && this.Children != null)
            {
                foreach (OctNode node in this.Children)
                {
                    if (node != null)
                        node.GetBucketsForLod(outBuckets, lod);
                }
            }
            else
            {
                List<Bucket> leafBuckets = new List<Bucket>();
                GetBuckets(leafBuckets);
                Bucket outBucket = new Bucket();
                outBucket.ipt = this.Loc;
                foreach (Bucket b in leafBuckets)
                {
                    outBucket.indices.AddRange(b.indices);
                }

                outBuckets.Add(outBucket);
            }
        }

        public void GetBuckets(List<Bucket> outBuckets)
        {
            if (this.Bucket != null)
                outBuckets.Add(this.Bucket);

            if (this.Children == null)
                return;

            foreach (OctNode node in this.Children)
            {
                if (node != null)
                    node.GetBuckets(outBuckets);
            }

        }

        public bool ClipYNrms()
        {
            if (this.Children == null)
            {
                if (Math.Abs(this.avgNrm.Y) > 0.2)
                    return true;
                else
                    return false;
            }

            if (this.Loc.Lod == 6)
            {
                bool keep = false;
                for (int idx = 0; idx < this.Children.Length; ++idx)
                {
                    if (this.Children[idx] == null)
                        continue;
                    if (Math.Abs(this.Children[idx].avgNrm.Y) > 0.2)
                    {
                        this.Children[idx] = null;
                    }
                    else
                        keep = true;
                }

                if (!keep)
                {
                    this.Children = null;
                }
            }
            else if (this.Children != null)
            {
                for (int idx = 0; idx < this.Children.Length; ++idx)
                {
                    if (this.Children[idx] != null)
                    {
                        if (this.Children[idx].ClipYNrms())
                            this.Children[idx] = null;
                    }
                }
            }

            return false;
        }
    }
}
