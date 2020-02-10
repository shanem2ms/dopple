using System;
using System.Collections.Generic;
using OpenTK;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dopple
{
    class DPEngine
    {
        [DllImport("dpengine.dll")]
        public static extern IntPtr CreatePtScorer(IntPtr pts0, uint ptClount0, IntPtr pts1, uint ptClount1, IntPtr matrix,
            int frameIdx);

        [DllImport("dpengine.dll")]
        public static extern float GetScore(IntPtr scorer, IntPtr pmatrix);

        [DllImport("dpengine.dll")]
        public static extern void FreePtScorer(IntPtr scorer);

        [DllImport("dpengine.dll")]
        public static extern IntPtr CreatePtCloudAlign(IntPtr m_pts0, uint ptCount0, IntPtr m_pts1, uint ptCount1);

        [DllImport("dpengine.dll")]
        public static extern int AlignStep(IntPtr aligner, IntPtr outmatrix);

        [DllImport("dpengine.dll")]
        public static extern void FreePtCloudAlign(IntPtr aligner);

        public static IntPtr AllocVec3Array(PtMesh.V3L []pos)
        {

            IntPtr mpts0 = Marshal.AllocHGlobal(pos.Length * 3 * sizeof(float));
            float[] vals = new float[pos.Length * 3];
            for (int idx = 0; idx < pos.Length; ++idx)
            {
                vals[idx * 3] = pos[idx].X;
                vals[idx * 3 + 1] = pos[idx].Y;
                vals[idx * 3 + 2] = pos[idx].Z;
            }
            Marshal.Copy(vals, 0, mpts0, vals.Length);
            return mpts0;
        }

        public static Matrix4 MatrixDToF(Matrix4d m)
        {
            return new Matrix4(Vector4DtoF(m.Row0),
                Vector4DtoF(m.Row1),
                Vector4DtoF(m.Row2),
                Vector4DtoF(m.Row3));
        }

        public static Vector4 Vector4DtoF(Vector4d v)
        {
            return new Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
        }
    }

    class AlgnNode
    {
        public AlgnNode(int i)
        { idx = i; }
        public float dist = 0;
        public int idx;
        public int matchedIdx = -1;
    }

    public class PTCloudAlignScore
    {
        IntPtr scorer;
        int frameIdx;
        public PTCloudAlignScore(PtMesh m0, PtMesh m1, Matrix4 fTransform, int frameIdx)
        {
            this.frameIdx = frameIdx;
            IntPtr mpts0 = DPEngine.AllocVec3Array(m0.pos);
            IntPtr mpts1 = DPEngine.AllocVec3Array(m1.pos);
            IntPtr mmatrix = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Matrix4)));
            Marshal.StructureToPtr(fTransform, mmatrix, false);

            this.scorer = DPEngine.CreatePtScorer(mpts0, (uint)m0.pos.Length * 3, mpts1, (uint)m1.pos.Length * 3,
                mmatrix, frameIdx);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
            Marshal.FreeHGlobal(mmatrix);
        }

        ~PTCloudAlignScore()
        {
            DPEngine.FreePtScorer(scorer);
        }
        public float GetScore(Matrix4 transform)
        {
            IntPtr mmatrix = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Matrix4)));
            Marshal.StructureToPtr(transform, mmatrix, false);
            float score = DPEngine.GetScore(scorer, mmatrix);
            Marshal.FreeHGlobal(mmatrix);
            return score;
        }
    }

    public class PtCldAlignNative
    {
        IntPtr aligner = IntPtr.Zero;
        public PtCldAlignNative(PtMesh m0, PtMesh m1)
        {
            IntPtr mpts0 = DPEngine.AllocVec3Array(m0.pos);
            IntPtr mpts1 = DPEngine.AllocVec3Array(m1.pos);
            this.aligner = DPEngine.CreatePtCloudAlign(mpts0, (uint)m0.pos.Length * 3, mpts1, (uint)m1.pos.Length * 3);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
        }

        public int AlignStep(out Matrix4 transform)
        {
            IntPtr mmatrix = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Matrix4)));
            int retval = DPEngine.AlignStep(this.aligner, mmatrix);
            transform = (Matrix4)Marshal.PtrToStructure(mmatrix, typeof(Matrix4));
            return retval;
        }

        ~PtCldAlignNative()
        {
            DPEngine.FreePtCloudAlign(this.aligner);
        }
    }
}
