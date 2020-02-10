// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "kdtree++/kdtree.hpp"
#include <gmtl/gmtl.h>
#include <gmtl/Vec.h>
#include <gmtl/Matrix.h>
#include <vector>

inline bool IsDepthValValid(float val)
{
    return !isnan(val) && val != 0;
}

template <int B, typename T> void BlurLine(T *vals, int begin, int end, int stride)
{
    float buf[B];
    for (int i = 0; i < B; ++i) buf[i] = NAN;
    int bufidx = 0;
    float accum = 0;
    float numvals = 0;
    for (int o = begin; o != end; o += stride, bufidx = (bufidx + 1) % B)
    {
        float val = vals[o];
        float prevval = buf[bufidx];
        buf[bufidx] = val;

        if (IsDepthValValid(prevval))
        {
            accum -= prevval;
            numvals -= 1.0f;
        }
        if (IsDepthValValid(val))
        {
            accum += val;
            numvals += 1.0f;
            vals[o] = accum / numvals;
        }
    }
}

template <int B, int P, typename T> void Blur(T* vals, int depthWidth, int depthHeight)
{
    int npasses = P;
    for (int i = 0; i < npasses; ++i)
    {
        for (int y = 0; y < depthHeight; ++y)
        {
            BlurLine<B, T>(vals, y * depthWidth, (y + 1) * depthWidth, 1);
            BlurLine<B, T>(vals, (y + 1) * depthWidth - 1, y * depthWidth - 1, -1);
        }

        for (int x = 0; x < depthWidth; ++x)
        {
            BlurLine<B, T>(vals, x, depthHeight * depthWidth + x, depthWidth);
            BlurLine<B, T>(vals, (depthHeight - 1) * depthWidth + x, x - depthWidth, -depthWidth);
        }
    }
}


template <int B, typename T> void SpreadInvalidsLine(T* vals, int begin, int end, int stride)
{
    bool buf[B];
    for (int i = 0; i < B; ++i) buf[i] = false;
    int bufidx = 0;
    int numinvalids = B;
    for (int o = begin; o != end; o += stride, bufidx = (bufidx + 1) % B)
    {
        bool isvalid = IsDepthValValid(vals[o]);
        bool lastvalid = buf[bufidx];
        buf[bufidx] = isvalid;
        if (!isvalid) numinvalids++;
        if (!lastvalid) numinvalids--;
        if (numinvalids > 0)
        {
            vals[o] = NAN;
        }
    }
}

template <int B, int P, typename T> void SpreadInvalids(T* vals, int depthWidth, int depthHeight)
{
    int npasses = P;
    for (int i = 0; i < npasses; ++i)
    {
        for (int y = 0; y < depthHeight; ++y)
        {
            SpreadInvalidsLine<B, T>(vals, y * depthWidth, (y + 1) * depthWidth, 1);
            SpreadInvalidsLine<B, T>(vals, (y + 1) * depthWidth - 1, y * depthWidth - 1, -1);
        }

        for (int x = 0; x < depthWidth; ++x)
        {
            SpreadInvalidsLine<B, T>(vals, x, depthHeight * depthWidth + x, depthWidth);
            SpreadInvalidsLine<B, T>(vals, (depthHeight - 1) * depthWidth + x, x - depthWidth, -depthWidth);
        }
    }
}



extern "C" {
    __declspec (dllexport) void DepthFindEdges(float *vals, int depthWidth, int depthHeight)
    {        
        std::vector<float> invalids(depthWidth * depthHeight);
        memcpy(&invalids[0], vals, invalids.size() * sizeof(float));
        SpreadInvalids<4, 3>(&invalids[0], depthWidth, depthHeight);
        for (int idx = 0; idx < depthHeight * depthWidth; idx++)
        {
            if (IsDepthValValid(vals[idx]))
                vals[idx] = 1.0f / vals[idx];
        }

        std::vector<float> origvals;
        origvals.resize(depthWidth * depthHeight);
        memcpy(&origvals[0], vals, origvals.size() * sizeof(float));

        Blur<2, 1>(vals, depthWidth, depthHeight);

        std::vector<float> vals2;
        vals2.resize(depthWidth * depthHeight);
        memcpy(&vals2[0], vals, vals2.size() * sizeof(float));
        Blur<2, 3>(&vals2[0], depthWidth, depthHeight);


        float diffthresh = 0.005f;
        for (int idx = 0; idx < depthHeight * depthWidth; idx++)
        {
            if (IsDepthValValid(vals2[idx]) && IsDepthValValid(vals[idx]))
            {
                vals[idx] = fabs(vals2[idx] - vals[idx]) > diffthresh ?
                    origvals[idx] : NAN;
            }
        }

        for (int idx = 0; idx < depthHeight * depthWidth; idx++)
        {
            if (!IsDepthValValid(invalids[idx]))
                vals[idx] = NAN;
            else if (IsDepthValValid(vals[idx]))
                vals[idx] = 1.0f / vals[idx];
        }
    }

    __declspec (dllexport) void ImageFindEdges(unsigned char* vals, int videoWidth, int videoHeight)
    {
        std::vector<unsigned char> yData(videoWidth * videoHeight);

        memcpy(&yData[0], vals, yData.size());

        Blur<2, 1>(&yData[0], videoWidth, videoHeight);
        std::vector<unsigned char> yData2(yData);
        Blur<2, 2>(&yData2[0], videoWidth, videoHeight);
        for (int idx = 0; idx < videoWidth * videoHeight; idx++)
        {
            vals[idx] = abs((int)yData[idx] - (int)yData2[idx]) > 5 ? 240 : 0;
        }
    }
}