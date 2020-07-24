#pragma once

#include <vector>
#include <gmtl/gmtl.h>
#include <gmtl/Vec.h>
#include <gmtl/Matrix.h>
#include <gmtl/Point.h>
#include <gmtl/AABox.h>

using namespace gmtl;

Matrix44f CameraMat(const Vec4f& cameraCalibrationVals,
    const Vec2f& cameraCalibrationDims,
    int dw, int dh);

extern "C"
{
    void AddWorldPointsToTree(const std::vector<Point3f>& pts);
    void CalcDepthPts(const Matrix44f& camMatrix, float* depthVals, int width, int height, Point3f* pOutPts);
    __declspec (dllexport) void DepthBuildLods(float* dbuf, float* outpts, int depthWidth, int depthHeight);
}

inline void AAExpand(AABoxf& aabox, const Point3f& v)
{
    for (int i = 0; i < 3; ++i)
    {
        if (v[i] < aabox.mMin[i])
            aabox.mMin[i] = v[i];
        if (v[i] > aabox.mMax[i])
            aabox.mMax[i] = v[i];
    }
}
