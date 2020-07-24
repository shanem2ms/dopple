// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
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
#include "OctTree.h"


typedef OctTree<Point3f, 64, 9> SceneTree;
static SceneTree g_sceneTree;
static AABoxf g_scenebounds(Vec3f(-20, -20, -20), Vec3f(20, 20, 20));
static bool g_isInitialized = false;

extern "C"
{
    __declspec (dllexport) int GetWorldNumPts();
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
}