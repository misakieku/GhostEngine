    public static void LockBoundary(UnsafeList<byte> locks, UnsafeList<UnsafeList<int>> groups, UnsafeList<Cluster> clusters, UnsafeList<uint> remap, byte* vertexLock)
    {
        for (int i = 0; i < (int)locks.Length; i++)
        {
            locks[i] &= ~((byte)((1 << 0) | (1 << 7)));
        }

        for (int i = 0; i < (int)groups.Length; i++)
        {
            // Mark remapped vertices
            for (int j = 0; j < (int)groups[i].Length; j++)
            {
                var cluster = clusters[groups[i][j]];
                for (int k = 0; k < (int)cluster.indices.Length; k++)
                {
                    uint v = cluster.indices[k];
                    uint r = remap[(int)v];
                    locks[(int)r] |= (byte)(locks[(int)r] >> 7);
                }
            }

            // Mark seen
            for (int j = 0; j < (int)groups[i].Length; j++)
            {
                var cluster = clusters[groups[i][j]];
                for (int k = 0; k < (int)cluster.indices.Length; k++)
                {
                    uint v = cluster.indices[k];
                    uint r = remap[(int)v];
                    locks[(int)r] |= (byte)(1 << 7);
                }
            }
        }

        for (int i = 0; i < (int)locks.Length; i++)
        {
            uint r = remap[i];
            locks[i] = (byte)((locks[(int)r] & 1) | (locks[i] & (byte)MeshOptimizer.Api.meshopt_SimplifyVertex_Protect));
            if (vertexLock != null)
                locks[i] |= vertexLock[i];
        }
    }
