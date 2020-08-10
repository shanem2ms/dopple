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
        public static extern void BestFit(IntPtr pts0, IntPtr nrm0, uint ptCount0, IntPtr pts1, uint ptCount1, int dw, int dh, float maxDistThreshold, IntPtr outTransform);

        [DllImport("ptslib.dll")]
        public static extern bool BestFitAll(IntPtr pts0, IntPtr pts1, int dw, int dh, IntPtr camMatrix, float maxDistThreshold, IntPtr outAlignTransform);

        [DllImport("ptslib.dll")]
        public static extern void AddWorldPoints(IntPtr pts, int dw, int dh,
            IntPtr yuv, int vw, int vh, IntPtr camMatrix, IntPtr transform,
            int curFrame);

        [DllImport("ptslib.dll")]
        public static extern int GetWorldNumPts(int frameStart, int frameCount);

        [DllImport("ptslib.dll")]
        public static extern int GetWorldPoints(IntPtr outpts, int frameStart, int frameCount);

        [DllImport("ptslib.dll")]
        public static extern void CalcScores();

        [DllImport("ptslib.dll")]
        public static extern int FindMatches(IntPtr m_pts0, uint ptCount0, IntPtr m_pts1, uint ptCount1, float maxDistThreshold, IntPtr matches);


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

        public static void CopyToVec3Array(IntPtr mpts0, Vector3[] pos)
        {
            float[] vals = new float[pos.Length * 3];
            Marshal.Copy(mpts0, vals, 0, vals.Length);
            for (int idx = 0; idx < pos.Length; ++idx)
            {
                pos[idx] = new Vector3(vals[idx * 3],
                    vals[idx * 3 + 1],
                    vals[idx * 3 + 2]);
            }
        }

        public static void CopyToWorldPtArray(IntPtr mpts0, WorldPt[] wpts)
        {
            int wptSize = Marshal.SizeOf(typeof(WorldPt));

            IntPtr ptr = mpts0;
            for (int idx = 0; idx < wpts.Length; ++idx)
            {
                wpts[idx] = (WorldPt)Marshal.PtrToStructure(ptr, typeof(WorldPt));
                ptr = new IntPtr(ptr.ToInt64() + wptSize);
            }
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

    public struct RGBt
    {
        public byte R;
        public byte G;
        public byte B;
    };

    public struct WorldPt
    {
        public Vector3 pt;
        public Vector3 nrm;
        public float size;
        public RGBt color;
    };


    public class Aligner
    {
        public static int [] FindMatches(Vector3 []pts0, Vector3[]pts1)
        {
            IntPtr mpts0 = DPEngine.AllocVec3Array(pts0);
            IntPtr mpts1 = DPEngine.AllocVec3Array(pts1);
            IntPtr matches = Marshal.AllocHGlobal(sizeof(int) * pts0.Length * 2);
            int nmatches = DPEngine.FindMatches(mpts0, (uint)pts0.Length, mpts1, (uint)pts1.Length, App.Settings.MaxMatchDist, matches);
            
            int[] matchArray = new int[nmatches * 2];
            Marshal.Copy(matches, matchArray, 0, matchArray.Length);

            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
            Marshal.FreeHGlobal(matches);
            return matchArray;
        }

        public static void Align(Vector3[] pts0, Vector3[] nrm0, Vector3[] pts1,
            int dw, int dh, float maxDistThreshold,
            out Matrix4 alignTransform)
        {
            IntPtr mpts0 = DPEngine.AllocVec3Array(pts0);
            IntPtr mpts1 = DPEngine.AllocVec3Array(pts1);
            IntPtr nrmp0 = DPEngine.AllocVec3Array(nrm0);

            IntPtr transformPtr = Marshal.AllocHGlobal(sizeof(float) * 16);
            DPEngine.BestFit(mpts0, nrmp0, (uint)pts0.Length, mpts1, (uint)pts1.Length, dw, dh, maxDistThreshold, transformPtr);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
            Marshal.FreeHGlobal(nrmp0);
            alignTransform = (Matrix4)Marshal.PtrToStructure(transformPtr, typeof(Matrix4));
            Marshal.FreeHGlobal(transformPtr);
        }

        public static bool AlignBest(byte []depthData0, byte[] depthData1,
            float []cameraVals,
            int dw, int dh,
            float maxDistThreshold,
            out Matrix4 alignTransform)
        {
            IntPtr mpts0 = Marshal.AllocHGlobal(depthData0.Length);
            Marshal.Copy(depthData0, 0, mpts0, depthData0.Length);
            IntPtr mpts1 = Marshal.AllocHGlobal(depthData1.Length);
            Marshal.Copy(depthData1, 0, mpts1, depthData1.Length);
            IntPtr camPtr = Marshal.AllocHGlobal(sizeof(float) * cameraVals.Length);
            Marshal.Copy(cameraVals, 0, camPtr, cameraVals.Length);

            IntPtr transformPtr = Marshal.AllocHGlobal(sizeof(float) * 16);
            bool success = DPEngine.BestFitAll(mpts0, mpts1, dw, dh, camPtr, maxDistThreshold, transformPtr);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(mpts1);
            Marshal.FreeHGlobal(camPtr);
            alignTransform = (Matrix4)Marshal.PtrToStructure(transformPtr, typeof(Matrix4));
            Marshal.FreeHGlobal(transformPtr);
            return success;
        }

        public static void GetWorldPoints(out WorldPt[] outPts, int startFrame, int frameCount)
        {
            int numPts = DPEngine.GetWorldNumPts(startFrame, frameCount);
            IntPtr ptsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WorldPt)) * numPts);
            DPEngine.GetWorldPoints(ptsPtr, startFrame, frameCount);
            outPts = new WorldPt[numPts];
            DPEngine.CopyToWorldPtArray(ptsPtr, outPts);
            Marshal.FreeHGlobal(ptsPtr);
        }

        public static void AddWorldPoints(byte[] depthData,
            int dw, int dh,
            byte []yuv,
            int vw, int vh,
            float[] cameraVals,
            Matrix4 transform,
            int curFrame)
        {
            IntPtr mpts0 = Marshal.AllocHGlobal(depthData.Length);
            Marshal.Copy(depthData, 0, mpts0, depthData.Length);
            IntPtr yuvPtr = Marshal.AllocHGlobal(yuv.Length);
            Marshal.Copy(yuv, 0, yuvPtr, yuv.Length);

            IntPtr camPtr = Marshal.AllocHGlobal(sizeof(float) * cameraVals.Length);
            Marshal.Copy(cameraVals, 0, camPtr, cameraVals.Length);
            IntPtr transformPtr = Marshal.AllocHGlobal(sizeof(float) * 16);
            Marshal.StructureToPtr(transform, transformPtr, false);

            DPEngine.AddWorldPoints(mpts0, dw, dh, yuvPtr, 
                vw, vh, camPtr, transformPtr, curFrame);
            Marshal.FreeHGlobal(yuvPtr);
            Marshal.FreeHGlobal(mpts0);
            Marshal.FreeHGlobal(camPtr);
            Marshal.FreeHGlobal(transformPtr);
        }


    }
}
