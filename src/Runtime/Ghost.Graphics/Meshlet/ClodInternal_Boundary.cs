using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Meshlet;

internal static class ClodBoundary
{
    public static unsafe void LockBoundary(UnsafeList<byte> locks, UnsafeList<UnsafeList<int>> groups, UnsafeList<Cluster> clusters, UnsafeList<uint> remap, byte* vertexLock)
    {
        byte* pLocks = (byte*)locks.GetUnsafePtr();
        uint* pRemap = (uint*)remap.GetUnsafePtr();

        for (int i = 0; i < locks.Count; i++)
            pLocks[i] = unchecked((byte)(pLocks[i] & ~((1 << 0) | (1 << 7))));

        for (int i = 0; i < groups.Count; i++)
        {
            for (int j = 0; j < groups[i].Count; j++)
            {
                var cluster = clusters[groups[i][j]];
                for (int k = 0; k < cluster.indices.Count; k++)
                {
                    uint r = pRemap[(int)cluster.indices[k]];
                    pLocks[r] |= (byte)(pLocks[r] >> 7);
                }
            }

            for (int j = 0; j < groups[i].Count; j++)
            {
                var cluster = clusters[groups[i][j]];
                for (int k = 0; k < cluster.indices.Count; k++)
                {
                    uint r = pRemap[(int)cluster.indices[k]];
                    pLocks[r] |= (byte)(1 << 7);
                }
            }
        }

        for (int i = 0; i < locks.Count; i++)
        {
            uint r = pRemap[i];
            pLocks[i] = (byte)((pLocks[r] & 1) | (pLocks[i] & (byte)(Api.meshopt_SimplifyVertex_Protect & 0xFF)));
            if (vertexLock != null)
                pLocks[i] |= vertexLock[i];
        }
    }
}
