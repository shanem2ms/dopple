// dllmain.cpp : Defines the entry point for the DLL application.
#include <cmath>
#include <vector>
#include <set>
#include <map>
#include <memory>
#include <algorithm>
#include "Pt.h"
#include <gmtl/gmtl.h>
#include <gmtl/Vec.h>
#include <gmtl/Matrix.h>
#include <vector>
#include "OctGrid.h"

using namespace gmtl;

typedef unsigned int uint;
typedef unsigned long ulong;

struct IPt

{
public:
    IPt(int lod, uint x, uint y, uint z)
    {
        X = x; Y = y; Z = z; Lod = lod;
    }

    int Lod;
    uint X;
    uint Y;
    uint Z;

    static Vec3f minVal;
    static float extents;
    static float extentsInv;

    static ulong CellCount(int lod)
    {
        ulong cldm = (ulong)(1 << lod);
        return (ulong)(cldm * cldm * cldm);
    }

    static float CellSize(int lod)
    {
        int cellCt = 1 << lod;
        return extents / (float)cellCt;
    }

    ulong CellIndex() const
    {
        ulong cldm = (ulong)(1 << Lod);
        return (ulong)(cldm * cldm * X +
            cldm * Y + Z);
    }

    static IPt FromVec(Vec3f vec, int lod)
    {
        float cellCt = 1 << lod;
        Vec3f nrmPt = (vec - minVal) * extentsInv;
        IPt ipt = IPt(lod, (uint)(nrmPt[0] * cellCt),
            (uint)(nrmPt[1] * cellCt),
            (uint)(nrmPt[2] * cellCt));
        return ipt;
    }

    IPt GetParentAtLod(int lod)
    {
        if (lod >= this->Lod)
            return *this;

        int levelShift = (this->Lod - lod);
        return IPt(lod, X >> levelShift,
            Y >> levelShift,
            Z >> levelShift);
    }

    IPt GetChild(int idx)
    {
        uint xInc = (uint)(idx & 1);
        uint yInc = (uint)((idx >> 1) & 1);
        uint zInc = (uint)((idx >> 2) & 1);
        return IPt(Lod + 1, (X << 1) + xInc,
            (Y << 1) + yInc,
            (Z << 1) + zInc);
    }

    int IndexOfChild(IPt child)
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

    void GetCellBounds(Vec3f &min, Vec3f &max)
    {
        int cellCt = 1 << this->Lod;
        float cellSize = extents / (float)cellCt;
        min = Vec3f(cellSize * X + minVal[0],
            cellSize * Y + minVal[1],
            cellSize * Z + minVal[2]);
        max = Vec3f(cellSize * (X + 1) + minVal[0],
            cellSize * (Y + 1) + minVal[1],
            cellSize * (Z + 1) + minVal[2]);
    }
    Vec3f GetCellCenter()
    {
        int cellCt = 1 << this->Lod;
        float cellSize = extents / (float)cellCt;
        return Vec3f(cellSize * (X + 0.5f) + minVal[0],
            cellSize * (Y + 0.5f) + minVal[1],
            cellSize * (Z + 0.5f) + minVal[2]);
    }

    void GetNeighbors(IPt neighbors[6])
    {
        neighbors[0] = IPt(this->Lod, this->X - 1, this->Y, this->Z);
        neighbors[1] = IPt(this->Lod, this->X + 1, this->Y, this->Z);
        neighbors[2] = IPt(this->Lod, this->X, this->Y - 1, this->Z);
        neighbors[3] = IPt(this->Lod, this->X, this->Y + 1, this->Z);
        neighbors[4] = IPt(this->Lod, this->X, this->Y, this->Z - 1);
        neighbors[5] = IPt(this->Lod, this->X, this->Y, this->Z + 1);
    }

    ulong DistSqFromCenter()
    {
        long cellCtH = (long)CellCount(this->Lod) / 2;
        long dx = ((long)X - cellCtH);
        long dy = ((long)Y - cellCtH);
        long dz = ((long)Z - cellCtH);
        return (ulong)(dx * dx) + (ulong)(dy * dy) + (ulong)(dz + dz);
    }

    ulong DistSqFrom(IPt other)
    {
        long dx = ((long)X - (long)other.X);
        long dy = ((long)Y - (long)other.Y);
        long dz = ((long)Z - (long)other.Z);
        return (ulong)(dx * dx) + (ulong)(dy * dy) + (ulong)(dz + dz);
    }

    ulong DistSqFrom(std::vector<IPt> others)
    {
        ulong totalDist = 1;
        for (const IPt &ipt : others)
        {
            totalDist *= DistSqFrom(ipt);
        }

        return totalDist;
    }

    bool operator < (const IPt &other)
    {
        if (Lod != other.Lod)
            return Lod < other.Lod;
        if (X != other.X)
            return X < other.X;
        if (Y != other.Y)
            return Y < other.Y;
        if (Z != other.Z)
            return Z < other.Z;
        return 0;

    }
};

Vec3f IPt::minVal(-1, -1, -1);
float IPt::extents = 2;
float IPt::extentsInv = 1.0f / extents;
