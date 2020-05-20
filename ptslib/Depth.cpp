// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <cmath>
#include <vector>
#include <map>
#include <memory>
#include <algorithm>
#include "Pt.h"

extern "C"
{
    Pt* tmpNrm = nullptr;
    static float threshhold = .75;
    __declspec (dllexport) void DepthFindNormals(float* vals, float* outpts, int px, int py, int depthWidth, int depthHeight)
    {
        tmpNrm = new Pt[depthWidth * depthHeight];


        Pt* depthPts = (Pt*)vals;
        for (int y = 1; y < depthHeight - 1; ++y)
        {
            for (int x = 1; x < depthWidth - 1; ++x)
            {
                Pt& ptx1 = depthPts[y * depthWidth + x + 1];
                Pt& ptx2 = depthPts[y * depthWidth + x - 1];
                Pt& pty1 = depthPts[(y - 1) * depthWidth + x];
                Pt& pty2 = depthPts[(y + 1) * depthWidth + x];
                Pt& outPt = tmpNrm[y * depthWidth + x];
                if (ptx1.IsValid() && ptx2.IsValid() &&
                    pty1.IsValid() && pty2.IsValid())
                {
                    Pt dx = ptx1 - ptx2;
                    Pt dy = pty1 - pty2;
                    outPt = Cross(dx, dy);
                    outPt.Normalize();
                }
                else
                {
                    outPt = Pt(0, 0, 0);
                }

            }
        }

        if (px >= 0)
        {
            Pt* outNrm = (Pt*)outpts;
            for (int y = 1; y < depthHeight - 1; ++y)
            {
                for (int x = 1; x < depthWidth - 1; ++x)
                {
                    outNrm[y * depthWidth + x] = Pt(0.4f, 0.4f, 0.4f);
                }
            }

            outNrm[py * depthWidth + px] = Pt(1, 1, 1);
        }
        else
        {
            Pt* outNrm = (Pt*)outpts;
            for (int y = 1; y < depthHeight - 1; ++y)
            {
                for (int x = 1; x < depthWidth - 1; ++x)
                {
                    Pt nrm = tmpNrm[y * depthWidth + x];
                    nrm += Pt(1, 1, 1);
                    nrm *= 0.5f;
                    outNrm[y * depthWidth + x] = nrm;
                }
            }
        }
    }
}

extern "C"
{
    __declspec (dllexport) void DepthBlur(float* dbuf, float* outpts, int depthWidth, int depthHeight, int blur)
    {
        std::vector<float> dinv;
        dinv.reserve(depthWidth * depthHeight);
        float* dend = dbuf + (depthWidth * depthHeight);
        for (float* dptr = dbuf; dptr != dend; ++dptr)
        {
            if (std::isnan(*dptr) || std::isinf(*dptr))
                dinv.push_back(NAN);
            else
                dinv.push_back(1.0f / *dptr);
        }

        for (int passIdx = 0; passIdx < blur; ++passIdx)
        {
            std::vector<float> doutx;
            doutx.resize(depthWidth * depthHeight);

            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    float w = 0;
                    float a = 0;
                    for (int c = -1; c <= 1; ++c)
                    {
                        int xc = x + c;
                        if (xc >= 0 && xc < depthWidth)
                        {
                            float v = dinv[y * depthWidth + xc];
                            if (!std::isnan(v) && !std::isinf(v))
                            {
                                w += 1.0f;
                                a += v;
                            }
                        }
                    }
                    if (w == 0)
                        doutx[y * depthWidth + x] = NAN;
                    else
                        doutx[y * depthWidth + x] = a / w;
                }
            }

            std::vector<float> douty;
            douty.resize(depthWidth * depthHeight);
            for (int x = 0; x < depthWidth; ++x)
            {
                for (int y = 0; y < depthHeight; ++y)
                {
                    float w = 0;
                    float a = 0;
                    for (int c = -1; c <= 1; ++c)
                    {
                        int yc = y + c;
                        if (yc >= 0 && yc < depthHeight)
                        {
                            float v = doutx[yc * depthWidth + x];
                            if (!std::isnan(v) && !std::isinf(v))
                            {
                                w += 1.0f;
                                a += v;
                            }
                        }
                    }
                    if (w == 0)
                        douty[y * depthWidth + x] = NAN;
                    else
                        douty[y * depthWidth + x] = a / w;
                }
            }

            memcpy(dinv.data(), douty.data(), douty.size() * sizeof(float));

        }

        for (int x = 0; x < depthWidth; ++x)
        {
            for (int y = 0; y < depthHeight; ++y)
            {
                dinv[y * depthWidth + x] =
                    1.0f / dinv[y * depthWidth + x];
            }
        }
        memcpy(outpts, dinv.data(), dinv.size() * sizeof(float));
    }

    __declspec (dllexport) void ImageBlur(unsigned char* ibuf, unsigned char* outpts, int imageWidth, int imageHeight, int blur)
    {
        std::vector<float> dinv;
        dinv.reserve(imageWidth * imageHeight);
        {
            unsigned char* dend = ibuf + (imageWidth * imageHeight);
            for (unsigned char* dptr = ibuf; dptr != dend; ++dptr)
            {
                dinv.push_back(*dptr);
            }
        }

        for (int passIdx = 0; passIdx < blur; ++passIdx)
        {
            std::vector<float> doutx;
            doutx.resize(imageWidth * imageHeight);

            for (int y = 0; y < imageHeight; ++y)
            {
                for (int x = 0; x < imageWidth; ++x)
                {
                    float w = 0;
                    float a = 0;
                    for (int c = -1; c <= 1; ++c)
                    {
                        int xc = x + c;
                        if (xc >= 0 && xc < imageWidth)
                        {
                            float v = dinv[y * imageWidth + xc];
                            w += 1.0f;
                            a += v;
                        }
                    }
                    if (w == 0)
                        doutx[y * imageWidth + x] = NAN;
                    else
                        doutx[y * imageWidth + x] = a / w;
                }
            }

            std::vector<float> douty;
            douty.resize(imageWidth * imageHeight);
            for (int x = 0; x < imageWidth; ++x)
            {
                for (int y = 0; y < imageHeight; ++y)
                {
                    float w = 0;
                    float a = 0;
                    for (int c = -1; c <= 1; ++c)
                    {
                        int yc = y + c;
                        if (yc >= 0 && yc < imageHeight)
                        {
                            float v = doutx[yc * imageWidth + x];
                            if (!std::isnan(v) && !std::isinf(v))
                            {
                                w += 1.0f;
                                a += v;
                            }
                        }
                    }
                    if (w == 0)
                        douty[y * imageWidth + x] = NAN;
                    else
                        douty[y * imageWidth + x] = a / w;
                }
            }

            memcpy(dinv.data(), douty.data(), douty.size() * sizeof(float));

        }

        {
            unsigned char* dend = outpts + (imageWidth * imageHeight);
            auto itd = dinv.begin();
            for (unsigned char* dptr = outpts; dptr != dend; ++dptr, ++itd)
            {
                *dend = (unsigned char)(*itd);
            }
        }
    }

    __declspec (dllexport) void DepthBuildLods(float* dbuf, float* outpts, int depthWidth, int depthHeight)
    {
        std::vector<float> dinv;
        dinv.reserve(depthWidth * depthHeight);
        float* dend = dbuf + (depthWidth * depthHeight);
        for (float* dptr = dbuf; dptr != dend; ++dptr)
        {
            if (std::isnan(*dptr) || std::isinf(*dptr))
                dinv.push_back(NAN);
            else
                dinv.push_back(1.0f / *dptr);
        }

        int dsw = depthWidth;
        int dw = depthWidth / 2;
        int dh = depthHeight / 2;
        float* dsrc = dinv.data();
        float* dptr = outpts;
        while (dw >= 16)
        {
            for (int y = 0; y < dh; ++y)
            {
                for (int x = 0; x < dw; ++x)
                {
                    float v[4] = {
                        dsrc[(y * 2) * dsw + (x * 2)],
                        dsrc[(y * 2) * dsw + (x * 2) + 1],
                        dsrc[(y * 2 + 1) * dsw + (x * 2)],
                        dsrc[(y * 2 + 1) * dsw + (x * 2) + 1] };
                    float t = 0;
                    float vf = 0;
                    for (int i = 0; i < 4; ++i)
                    {
                        if (!std::isnan(v[i]))
                        {
                            vf += v[i];
                            t += 1.0f;
                        }
                    }
                    dptr[y * dw + x] = (t > 0) ? (vf / t) : NAN;
                }
            }

            dsrc = dptr;
            dptr += dw * dh;

            dw /= 2;
            dsw /= 2;
            dh /= 2;
        }

        for (float* ptr = outpts; ptr < dptr; ++ptr)
        {
            *ptr = !std::isnan(*ptr) ? 1.0f / *ptr : NAN;
        }
    }
}