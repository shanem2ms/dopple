// dllmain.cpp : Defines the entry point for the DLL application.
#include "OctTree.h"
#include "gmtl/Matrix.h"

static SceneTree g_sceneTree;
static AABoxf g_scenebounds(Vec3f(-20, -20, -20), Vec3f(20, 20, 20));
static bool g_isInitialized = false;


PTEXPORT int GetWorldNumPts(int frameStart, int frameCount)
{
    size_t numPts = 0;
    std::vector<const SceneTree::Node*> leafNodes;
    g_sceneTree.GetLeafNodes(leafNodes);
    int frameEnd = frameStart + frameCount;
    for (const SceneTree::Node* pNode : leafNodes)
    {
        for (auto pt : pNode->Pts())
        {
            if (pt.second.val.firstFrame >= frameStart &&
                pt.second.val.firstFrame < frameEnd)
                numPts++;
        }
    }

    return (int)numPts;
}

PTEXPORT void GetWorldPoints(WorldPt* outPts, int frameStart, int frameCount)
{
    std::vector<const SceneTree::Node*> leafNodes;
    g_sceneTree.GetLeafNodes(leafNodes);
    int frameEnd = frameStart + frameCount;

    for (const SceneTree::Node* pNode : leafNodes)
    {
        for (auto pt : pNode->Pts())
        {
            if (pt.second.val.firstFrame >= frameStart &&
                pt.second.val.firstFrame < frameEnd)
            {
                WorldPt& wpt = *(outPts++);
                wpt.pt = pt.second.loc;
                wpt.nrm = pt.second.val.normal;
                wpt.color = pt.second.val.color;
                wpt.size = 0.005f;
            }
        }
    }
}

inline RGBt GetRGBVal(int ix, int iy, int width, int height, byte* imageData)
{
    int uvWidth = width;
    int uvOffset = width * height;

    float m[9] = { 1, 1, 1,
        0, -0.18732f, 1.8556f,
        1.57481f, -0.46813f, 0 };
    Matrix33f matyuv;
    memcpy(matyuv.mData, m, sizeof(m));
    matyuv.mState = Matrix33f::FULL;
    byte yVal = imageData[iy * width + ix];
    int uvy = iy / 2;
    int uvx = ix & 0xFFFE;
    byte uVal = imageData[uvOffset + uvy * uvWidth + uvx];
    byte vVal = imageData[uvOffset + uvy * uvWidth + uvx + 1];
    Vec3f yuv(yVal / 255.0f, (uVal / 255.0f) - 0.5f, (vVal / 255.0f) - 0.5f);
    Vec3f rgb = matyuv * yuv;
    return RGBt((byte)(rgb[0] * 255.0f), (byte)(rgb[1] * 255.0f), (byte)(rgb[2] * 255.0f));
}

std::vector<byte> sData[2];

void ImgDownscale(int inwidth, int inheight, byte* yuv, int n, std::vector<byte>& outBytes)
{
    int width = inwidth, height = inheight;


    if (sData[0].size() == 0)
    {
        sData[0].resize((width * height * 3) / 2);
        sData[1].resize((width * height * 3) / 2);
    }

    for (int lod = 0; lod < n; ++lod)
    {
        int outWidth = width / 2;
        int outHeight = height / 2;

        byte* inbyts = lod == 0 ? yuv : sData[(lod - 1) % 2].data();
        byte* outbyts = sData[lod % 2].data();


        if (lod == n - 1)
        {
            outBytes.resize(outWidth * (outHeight + outHeight / 2));
            outbyts = outBytes.data();
        }

        for (int ih = 0; ih < outHeight; ++ih)
        {
            int h = ih * 2;
            for (int iw = 0; iw < outWidth; ++iw)
            {
                int w = iw * 2;
                int v = inbyts[h * width + w] +
                    inbyts[h * width + w + 1] +
                    inbyts[(h + 1) * width + w] +
                    inbyts[(h + 1) * width + w + 1];
                outbyts[ih * outWidth + iw] = v / 4;
            }
        }
        int uvOffset = width * height;
        int oUvOoffset = outWidth * outHeight;

        for (int ih = 0; ih < outHeight / 2; ++ih)
        {
            int h = ih * 2;
            for (int iw = 0; iw < outWidth; ++iw)
            {
                int w = iw * 2;
                int v = inbyts[uvOffset + h * width + w] +
                    inbyts[uvOffset + h * width + w + 1] +
                    inbyts[uvOffset + (h + 1) * width + w] +
                    inbyts[uvOffset + (h + 1) * width + w + 1];
                outbyts[oUvOoffset + ih * outWidth + iw] = v / 4;
            }
        }

        width = outWidth;
        height = outHeight;
    }
}

void AddWorldPointsToTree(const std::vector<Point3f>& pts, const std::vector<RGBt>& colors, const std::vector<Vec3f>& nrm, int curFrame);


PTEXPORT void AddWorldPoints(float* vals, int width, int height,
    uchar* yuv, int vwidth, int vheight,
    float* cameraVals,
    Matrix44f* transform, int curFrame)
{
    //std::vector<byte> imgDS;
    //ImgDownscale(vwidth, vheight, yuv, 2, imgDS);

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
    DepthBuildLods(vals, depthLods.data(), width, height, 2);

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
    CalcDepthPts(camMatrix, lodVals, dw, dh, pts0);

    for (auto itPt = pts0.begin(); itPt != pts0.end(); ++itPt)
    {
        gmtl::xform(*itPt, *transform, *itPt);
    }

    std::vector<Vec3f> normals;
    normals.resize(pts0.size());
    CalcNormals(pts0.data(), normals.data(), dw, dh);

    std::vector<RGBt> colorVals;
    for (int h = 0; h < dh; ++h)
    {
        for (int w = 0; w < dw; ++w)
        {
            int vx = (w * vwidth) / dw;
            int vy = (h * vheight) / dh;
            colorVals.push_back(GetRGBVal(vx, vy, vwidth, vheight, yuv));
        }
    }

    AddWorldPointsToTree(pts0, colorVals, normals, curFrame);
    /*
    std::ostringstream str;
    str << g_minmax << std::endl;
    OutputDebugStringA(str.str().c_str());*/
}

void AddWorldPointsToTree(const std::vector<Point3f>& pts, const std::vector<RGBt> &colors, 
    const std::vector<Vec3f>& normals, int curFrame)
{
    if (!g_isInitialized)
    {
        g_sceneTree.SetBounds(g_scenebounds);
        g_isInitialized = true;
    }

    auto itCol = colors.begin();
    auto itNrm = normals.begin();
    for (auto itPt = pts.begin(); itPt != pts.end(); ++itPt, ++itCol, ++itNrm)
    {
        if (!IsValid(*itPt))
            continue;
        OctPoint<TreePt> octPt(*itPt, TreePt(*itCol, *itNrm, curFrame));
        g_sceneTree.AddPoint(octPt);
    }

    //int numpts0 = GetWorldNumPts();
    g_sceneTree.ConsolidatePts();
    //int numpts1 = GetWorldNumPts();
}

