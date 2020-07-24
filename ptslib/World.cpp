// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "OctTree.h"


typedef OctTree<Point3f, 64, 9> SceneTree;
static SceneTree g_sceneTree;
static AABoxf g_scenebounds(Vec3f(-20, -20, -20), Vec3f(20, 20, 20));
static bool g_isInitialized = false;

extern "C"
{
    void AddWorldPointsToTree(const std::vector<Point3f>& pts)
    {
        if (!g_isInitialized)
        {
            g_sceneTree.SetBounds(g_scenebounds);
            g_isInitialized = true;
        }

        for (auto itPt = pts.begin(); itPt != pts.end(); ++itPt)
        {
            OctPoint<Point3f> octPt(*itPt, *itPt);
            g_sceneTree.AddPoint(octPt);
        }
        
        //int numpts0 = GetWorldNumPts();
        g_sceneTree.ConsolidatePts();
        //int numpts1 = GetWorldNumPts();
    }


    __declspec (dllexport) int GetWorldNumPts()
    { 
        size_t numPts = 0;
        std::vector<const SceneTree::Node*> leafNodes;
        g_sceneTree.GetLeafNodes(leafNodes);
        for (const SceneTree::Node* pNode : leafNodes)
        {
            numPts += pNode->Pts().size();
        }

        return (int)numPts;
    }

    __declspec (dllexport) void GetWorldPoints(Point3f *outPts, int nPts)
    {
        std::vector<const SceneTree::Node *> leafNodes;
        g_sceneTree.GetLeafNodes(leafNodes);

        for (const SceneTree::Node* pNode : leafNodes)
        {
            for (auto pt : pNode->Pts())
            {
                (*outPts++) = pt.second.val;
            }
        }
    }


    __declspec (dllexport) void AddWorldPoints(float* vals, int width, int height,
        float* cameraVals,
        Matrix44f* transform)
    {
        Vec4f cameraCalibrationVals;
        Vec2f cameraCalibrationDims;
        memcpy(&cameraCalibrationVals, cameraVals, sizeof(float) * 4);
        memcpy(&cameraCalibrationDims, cameraVals + 4, sizeof(float) * 2);

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


        std::vector<float> dlods[8];
        std::vector<float> depthLods;
        depthLods.resize(totalFloats);
        DepthBuildLods(vals, depthLods.data(), width, height);

        dw = width;
        dh = height;

        float* ptr = depthLods.data();
        size_t offset = 0;
        for (int i = 0; i < nLods; ++i)
        {
            dw /= 2;
            dh /= 2;

            dlods[i].resize(dw * dh);
            memcpy(dlods[i].data(), ptr + offset, dw * dh * sizeof(float));
            offset += dw * dh;
        }

        int curLod = 2;
        dw = width >> curLod;
        dh = height >> curLod;

        Matrix44f camMatrix =
            CameraMat(cameraCalibrationVals, cameraCalibrationDims, dw, dh);

        std::vector<Point3f> pts0;
        float* lodVals = dlods[curLod - 1].data();
        pts0.resize(dlods[curLod - 1].size());
        CalcDepthPts(camMatrix, lodVals, dw, dh, pts0.data());

        for (auto itPt = pts0.begin(); itPt != pts0.end(); ++itPt)
        {
            gmtl::xform(*itPt, *transform, *itPt);
        }

        AddWorldPointsToTree(pts0);
        /*
        std::ostringstream str;
        str << g_minmax << std::endl;
        OutputDebugStringA(str.str().c_str());*/
        AddWorldPointsToTree(pts0);
    }

}