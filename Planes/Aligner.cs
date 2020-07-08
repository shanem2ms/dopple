using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK;

namespace Planes
{
    class DPEngine
    {
        [DllImport("ptslib.dll")]
        public static extern IntPtr CreatePtCloudAlign(IntPtr m_pts0, uint ptCount0, IntPtr m_pts1, uint ptCount1);

        [DllImport("ptslib.dll")]
        public static extern int GetNearest(IntPtr pts0, uint ptCount0, IntPtr pts1, uint ptCount1, IntPtr outMatches);

        [DllImport("ptslib.dll")]
        public static extern int AlignStep(IntPtr aligner, IntPtr outmatrix);

        [DllImport("ptslib.dll")]
        public static extern void FreePtCloudAlign(IntPtr aligner);

        [DllImport("ptslib.dll")]
        public static extern void BestFit(IntPtr pts0, uint ptCount0, IntPtr pts1, uint ptCount1, int dw, int dh, IntPtr outTranslate,
            IntPtr outRotate);

        [DllImport("ptslib.dll")]
        public static extern void CalcScores();

        [DllImport("ptslib.dll")]
        public static extern int FindMatches(IntPtr m_pts0, uint ptCount0, IntPtr m_pts1, uint ptCount1, IntPtr matches);


        public static IntPtr AllocVec3Array(Vector3[] pos)
        {

            IntPtr mpts0 = Marshal.AllocHGlobal(pos.Length * 3 * sizeof(float));
            CopyVec3Array(pos, mpts0);
            return mpts0;
        }

        public static void CopyVec3Array(Vector3[] pos, IntPtr mpts0)
        {
            float[] vals = new float[pos.Length * 3];
            for (int idx = 0; idx < pos.Length; ++idx)
            {
                vals[idx * 3] = pos[idx].X;
                vals[idx * 3 + 1] = pos[idx].Y;
                vals[idx * 3 + 2] = pos[idx].Z;
            }
            Marshal.Copy(vals, 0, mpts0, vals.Length);
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

    public class Aligner
    {
        public static int [] FindMatches(Vector3 []pts0, Vector3[]pts1)
        {
            IntPtr mpts0 = DPEngine.AllocVec3Array(pts0);
            IntPtr mpts1 = DPEngine.AllocVec3Array(pts1);
            IntPtr matches = Marshal.AllocHGlobal(sizeof(int) * pts0.Length * 2);
            int nmatches = DPEngine.FindMatches(mpts0, (uint)pts0.Length, mpts1, (uint)pts1.Length, matches);
            
            int[] matchArray = new int[nmatches * 2];
            Marshal.Copy(matches, matchArray, 0, matchArray.Length);

            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
            Marshal.FreeHGlobal(matches);
            return matchArray;
        }

        public static void Align(Vector3[] pts0, Vector3[] nrm0, Vector3[] pts1,
            int dw, int dh,
            out Vector3 offset,
            out Vector3 eRot)
        {
            IntPtr mpts0 = DPEngine.AllocVec3Array(pts0);
            IntPtr mpts1 = DPEngine.AllocVec3Array(pts1);


            IntPtr translatePtr = Marshal.AllocHGlobal(sizeof(float) * 3);
            IntPtr rotatePtr = Marshal.AllocHGlobal(sizeof(float) * 3);
            DPEngine.BestFit(mpts0, (uint)pts0.Length, mpts1, (uint)pts1.Length, dw, dh, translatePtr, rotatePtr);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
            offset = (Vector3)Marshal.PtrToStructure(translatePtr, typeof(Vector3));
            eRot = (Vector3)Marshal.PtrToStructure(rotatePtr, typeof(Vector3));
            Marshal.FreeHGlobal(translatePtr);
            Marshal.FreeHGlobal(rotatePtr);
        }
    }
}
