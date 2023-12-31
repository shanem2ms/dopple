// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "kdtree++/kdtree.hpp"
#include <gmtl/gmtl.h>
#include <gmtl/Vec.h>
#include <gmtl/Matrix.h>
#include <vector>

using namespace gmtl;

typedef float vfloat;
typedef Point3f Point3v;
typedef Matrix44f Matrix44v;
typedef Vec3f Vec3v;
typedef AxisAnglef AxisAnglev;
typedef Quatf Quatv;

struct V3
{
    typedef vfloat value_type;
    Point3v pos;
    value_type operator[](size_t n) const
    {
        return pos[n];
    }
};
struct ANode
{
    typedef vfloat value_type;

    ANode(const Point3v& p, size_t i) :
        pos(p),
        idx(i) {}
    
    Point3v pos;
    size_t idx;

    value_type operator[](size_t n) const
    {
        return pos[n];
    }
};

struct Match
{
    Match() : distsq(-1), matchedIdx(-1) {}
    vfloat distsq;
    size_t idx;
    long matchedIdx;
};

class PtScorer
{
    
    std::vector<Point3v> m_pts0;
    std::vector<Match> m_matches;
    std::vector<Point3v> m_pts1;
    std::vector<Point3v> m_tpts1;
    Matrix44v m_transform;
    KDTree::KDTree <3, ANode> m_kdtree;
    bool m_isinit;
    int m_frameIdx;

public:
    PtScorer(Vec3v* pts0, size_t npts0, Vec3v* pts1, size_t npts1, Matrix44v* pMat,
        int frameIdx) :
        m_isinit(false),
        m_frameIdx(frameIdx)
    {
        m_pts0.resize(npts0);
        memcpy(&m_pts0[0], pts0, npts0 * sizeof(Point3v));
        m_pts1.resize(npts1);
        memcpy(&m_pts1[0], pts1, npts1 * sizeof(Point3v));
        m_transform = *pMat;
        m_transform.mState = Matrix44v::XformState::AFFINE;
    }

    vfloat GetScore(Matrix44v& testTransform)
    {
        if (!m_isinit)
            Init();
        vfloat resultscore = 0;
        for (const Match& match : m_matches)
        {
            Point3v& pt0 = m_pts0[match.idx];
            Point3v& pt1 = m_pts1[match.matchedIdx];
            Point3v tpt1;
            xform(tpt1, testTransform, pt1);
            Vec3v v3f = tpt1 - pt0;
            resultscore += lengthSquared(v3f);
        }
        resultscore /= (vfloat)m_matches.size();
        return log10(1.0 / resultscore);
    }

    ~PtScorer()
    {
        m_pts0.clear();
    }

private:
    void Init()
    {
        size_t ptidx = 0;
        for (Point3v& pt : m_pts0)
        {
            ANode an(pt, ptidx++);
            m_kdtree.insert(an);
        }

        m_kdtree.optimize();
        m_tpts1.resize(m_pts1.size());
        auto ittpt = m_tpts1.begin();
        for (const Point3v& pt1 : m_pts1)
        {
            xform(*ittpt, m_transform, pt1);
            ++ittpt;
        }

        size_t midx = 0;
        std::vector<Match> matches;
        matches.resize(m_pts0.size());
        for (Match& m : matches) { m.idx = midx++; }

        size_t mIdx = 0;
        for (const Point3v& tpt : m_tpts1)
        {
            ANode v3(tpt, 0);
            auto itFound = m_kdtree.find_nearest(v3);
            Match& match = matches[itFound.first->idx];
            const Point3v& pt0 = m_pts0[itFound.first->idx];
            Vec3v v3f = tpt - pt0;
            vfloat distsq = lengthSquared(v3f);
            if (match.matchedIdx < 0 ||
                distsq < match.distsq)
            {
                match.distsq = distsq;
                match.matchedIdx = mIdx;
            }

            mIdx++;
        }

        for (const Match &match : matches)
        {
            if (match.matchedIdx >= 0)
                m_matches.push_back(match);
        }

        m_isinit = true;
    }
};


class PTCloudAlign
{

    Matrix44v m_prevTransform;
    Matrix44v m_curTransform;
    int m_curstepsize = -1;

    std::vector<Point3v> m_pts0;
    std::vector<Point3v> m_pts1;
    vfloat m_previousScore = 0;

    KDTree::KDTree <3, ANode> m_kdtree;
    std::vector<ANode> m_allNodes;
    std::vector<Match> m_matches;

public:

    PTCloudAlign(Vec3v* ipts0, size_t npts0, Vec3v* ipts1, size_t npts1)
    {
        m_pts0.resize(npts0);
        memcpy(&m_pts0[0], ipts0, sizeof(Vec3v) * npts0);
        m_pts1.resize(npts1);
        memcpy(&m_pts1[0], ipts1, sizeof(Vec3v) * npts1);
    }

    static void GetDerivatives(
        const std::vector<Point3v> &ptsStart,
        const std::vector<Point3v>& ptsEnd,
        Vec3v rotateVec,
        Vec3v translate,
        Vec3v &uScore,
        Vec3v &tScore)
    {
        vfloat tx = translate[0];
        vfloat ty = translate[1];
        vfloat tz = translate[2];
        
        vfloat cosrx = (vfloat)cos(rotateVec[0]);
        vfloat sinrx = (vfloat)sin(rotateVec[0]);
        vfloat cosry = (vfloat)cos(rotateVec[1]);
        vfloat sinry = (vfloat)sin(rotateVec[1]);
        vfloat cosrz = (vfloat)cos(rotateVec[2]);
        vfloat sinrz = (vfloat)sin(rotateVec[2]);
        Vec3v ud(0, 0, 0);
        Vec3v td(0, 0, 0);

        vfloat invlen = 1.0 / ptsStart.size();

        for (int i = 0; i < ptsStart.size(); ++i)
        {
            vfloat x_src = ptsEnd[i][0];
            vfloat y_src = ptsEnd[i][1];
            vfloat z_src = ptsEnd[i][2];
            vfloat x_dst = ptsStart[i][0];
            vfloat y_dst = ptsStart[i][1];
            vfloat z_dst = ptsStart[i][2];

            vfloat derivrx =
                (2 * (-tx + x_dst - cosry * x_src - sinrx * sinry * y_src - cosrx * sinry * z_src) * (-cosrx * sinry * y_src + sinrx * sinry * z_src) + 2 * (-(cosrx * cosry * cosrz - sinrx * sinrz) * y_src - (-cosry * cosrz * sinrx - cosrx * sinrz) * z_src) * (-tz + cosrz * sinry * x_src - (cosry * cosrz * sinrx + cosrx * sinrz) * y_src + z_dst - (cosrx * cosry * cosrz - sinrx * sinrz) * z_src) + 2 * (-ty - sinry * sinrz * x_src + y_dst - (cosrx * cosrz - cosry * sinrx * sinrz) * y_src - (-cosrz * sinrx - cosrx * cosry * sinrz) * z_src) * (-(-cosrz * sinrx - cosrx * cosry * sinrz) * y_src - (-cosrx * cosrz + cosry * sinrx * sinrz) * z_src));
            vfloat derivry =
                (2 * (sinry * x_src - cosry * sinrx * y_src - cosrx * cosry * z_src) * (-tx + x_dst - cosry * x_src - sinrx * sinry * y_src - cosrx * sinry * z_src) + 2 * (-cosry * sinrz * x_src - sinrx * sinry * sinrz * y_src - cosrx * sinry * sinrz * z_src) * (-ty - sinry * sinrz * x_src + y_dst - (cosrx * cosrz - cosry * sinrx * sinrz) * y_src - (-cosrz * sinrx - cosrx * cosry * sinrz) * z_src) + 2 * (cosry * cosrz * x_src + cosrz * sinrx * sinry * y_src + cosrx * cosrz * sinry * z_src) * (-tz + cosrz * sinry * x_src - (cosry * cosrz * sinrx + cosrx * sinrz) * y_src + z_dst - (cosrx * cosry * cosrz - sinrx * sinrz) * z_src));
            vfloat derivrz =
                (2 * (-sinry * sinrz * x_src - (cosrx * cosrz - cosry * sinrx * sinrz) * y_src - (-cosrz * sinrx - cosrx * cosry * sinrz) * z_src) * (-tz + cosrz * sinry * x_src - (cosry * cosrz * sinrx + cosrx * sinrz) * y_src + z_dst - (cosrx * cosry * cosrz - sinrx * sinrz) * z_src) + 2 * (-ty - sinry * sinrz * x_src + y_dst - (cosrx * cosrz - cosry * sinrx * sinrz) * y_src - (-cosrz * sinrx - cosrx * cosry * sinrz) * z_src) * (-cosrz * sinry * x_src - (-cosry * cosrz * sinrx - cosrx * sinrz) * y_src - (-cosrx * cosry * cosrz + sinrx * sinrz) * z_src));
            vfloat derivtx =
                -2 * (-tx + x_dst - cosry * x_src - sinrx * sinry * y_src - cosrx * sinry * z_src);
            vfloat derivty =
                -2 * (-ty - sinry * sinrz * x_src + y_dst - (cosrx * cosrz - cosry * sinrx * sinrz) * y_src - (-cosrz * sinrx - cosrx * cosry * sinrz) * z_src);
            vfloat derivtz =
                -2 * (-tz + cosrz * sinry * x_src - (cosry * cosrz * sinrx + cosrx * sinrz) * y_src + z_dst - (cosrx * cosry * cosrz - sinrx * sinrz) * z_src);

            ud += Vec3v(derivrx, derivry, derivrz);
            td += Vec3v(derivtx, derivty, derivtz);
        }

        uScore = ud * invlen;
        tScore = td * invlen;
    }

    void BestFit(const std::vector<Point3v>& ptsStart,
        const std::vector<Point3v>& ptsEnd,
        Vec3v &translate,
        Vec3v &rotate)
    {
        Vec3v trans(0, 0, 0);
        Vec3v rot(0, 0, 0);
        vfloat r = 0;
        for (int i = 0; i < 100; ++i)
        {
            Vec3v uScore;
            Vec3v tScore;
            GetDerivatives(ptsStart, ptsEnd, rot, trans, uScore, tScore);
            vfloat totalScore = fabs(length(uScore) + length(tScore));
            trans -= tScore * 0.5f;
            rot[0] -= uScore[0] * 2.0f;
            rot[1] -= uScore[1] * 2.0f;
            rot[2] -= uScore[2] * 2.0f;
        }

        translate = trans;
        rotate = rot;
    }

    int AlignStep(Matrix44v &outTransform)
    {
        if (m_curstepsize < 0)
        {
            m_matches.resize(m_pts0.size());
            size_t midx = 0;
            for (Match& m : m_matches) { m.idx = midx++; }

            int ptidx = 0;
            for (Point3v& pt : m_pts0)
            {
                ANode an(pt, ptidx);
                m_allNodes.push_back(an);
                m_kdtree.insert(an);
                ptidx++;
            }

            m_kdtree.optimize();
            m_curstepsize = m_pts0.size() / 10;
        }

        for (Match& m : m_matches)
        {
            m.matchedIdx = -1;
            m.distsq = 0;
        }

        std::vector<Point3v> pts1t;
        Matrix44v transfrm = m_curTransform;
        for (size_t idx = 0; idx < m_pts1.size(); idx += m_curstepsize)
        {
            Point3v tpt;
            xform(tpt, transfrm, m_pts1[idx]);
            pts1t.push_back(tpt);
        }

        size_t mIdx = 0;
        for (const Point3v& tpt : pts1t)
        {
            const ANode& v3 = (const ANode&)tpt;
            auto itFound = m_kdtree.find_nearest(v3);
            Match& match = m_matches[itFound.first->idx];
            const Point3v& pt0 = m_pts0[itFound.first->idx];
            Vec3v v3f = tpt - pt0;
            vfloat distsq = lengthSquared(v3f);
            if (match.matchedIdx < 0 ||
                distsq < match.distsq)
            {
                match.distsq = distsq;
                match.matchedIdx = mIdx;
            }

            mIdx++;
        }


        std::vector<Match> matches;
        for (const Match &match : m_matches)
        {
            if (match.matchedIdx >= 0)
                matches.push_back(match);
        }

        vfloat avgdistsq = 0;
        for (Match& match : matches)
        {
            match.distsq =
                lengthSquared(Vec3v(m_pts0[match.idx] - pts1t[match.matchedIdx]));
        }

        avgdistsq /= (vfloat)matches.size();
        avgdistsq *= 4.0;
       
        for (auto itMatch = matches.begin(); itMatch != matches.end();)
        {
            if (itMatch->distsq < avgdistsq)
                itMatch = matches.erase(itMatch);
            else
                itMatch++;
        }

        std::vector<Point3v> vecs0;
        std::vector<Point3v> vecs1;
        for (Match& m : matches)
        {
            vecs0.push_back(m_pts0[m.idx]);
            vecs1.push_back(pts1t[m.matchedIdx]);
        }

        Vec3v translate2;
        Vec3v rotate2;
        BestFit(vecs0, vecs1, translate2, rotate2);
        
        AxisAnglev aaY(rotate2[1], Vec3v(0, 1, 0));
        AxisAnglev aaZ(rotate2[2], Vec3v(0, 0, 1));
        Quatv qx = make<Quatv, AxisAnglev>(AxisAnglev(rotate2[0], Vec3v(1, 0, 0)));
        Quatv qy = make<Quatv, AxisAnglev>(AxisAnglev(rotate2[1], Vec3v(0, 1, 0)));
        Quatv qz = make<Quatv, AxisAnglev>(AxisAnglev(rotate2[2], Vec3v(1, 0, 0)));
        Quatv q = qx * qy * qz;
        Matrix44v rotmat = makeRot<Matrix44v, Quatv>(q);
        Matrix44v trnsmat = makeTrans<Matrix44v, Vec3v>(translate2);
        Matrix44v transform = rotmat * trnsmat;


        vfloat resultscore = 0;
        for (int idx = 0; idx < vecs0.size(); idx++)
        {
            Point3v tpt1;
            xform(tpt1, transform, vecs1[idx]);
            resultscore += lengthSquared(Vec3v(vecs0[idx] - tpt1));
        }
        resultscore /= (vfloat)vecs0.size();
        resultscore = (vfloat)log10(1.0 / resultscore);

        int retstg;
        if (resultscore <= m_previousScore)
        {
            int nsteps = m_pts0.size() / m_curstepsize;
            m_curstepsize /= 2;
            m_previousScore = 0;
            m_prevTransform = m_curTransform;
            retstg = 1;
            if (nsteps > 100)
                retstg = 2;
        }
        else
        {
            m_curTransform = m_curTransform * transform;
            m_previousScore = resultscore;
            retstg = 0;
        }
        
        outTransform = m_curTransform;
        return retstg;
    }

};

extern "C" {

    __declspec (dllexport) PtScorer* CreatePtScorer(vfloat* m_pts0, size_t ptCount0, vfloat* m_pts1, size_t ptCount1, vfloat* pmatrix,
        int frameIdx)
    {
        return new PtScorer((Vec3v*)m_pts0, ptCount0 / 3, (Vec3v*)m_pts1, ptCount1 / 3, (Matrix44v*)pmatrix, frameIdx);
    }

    __declspec (dllexport) vfloat GetScore(PtScorer* pthis, vfloat* pmatrix)
    {
        return pthis->GetScore((Matrix44v&)* pmatrix);
    }

    __declspec (dllexport) void FreePtScorer(PtScorer* pthis)
    {
        delete pthis;
    }

    __declspec (dllexport) PTCloudAlign* CreatePtCloudAlign(vfloat* pts0, size_t ptCount0, vfloat* pts1, size_t ptCount1)
    {
        return new PTCloudAlign((Vec3v*)pts0, ptCount0 / 3, (Vec3v*)pts1, ptCount1 / 3);
    }

    __declspec (dllexport) int AlignStep(PTCloudAlign* pthis, vfloat *matrix)
    {
        return pthis->AlignStep((Matrix44v&)*matrix);
    }

    __declspec (dllexport) void FreePtCloudAlign(PTCloudAlign* pthis)
    {
        delete pthis;
    }

}

