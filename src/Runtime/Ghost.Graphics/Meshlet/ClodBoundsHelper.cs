using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal static class ClodBoundsHelper
{
    public static ClodBounds ComputeBounds(ClodMesh mesh, UnsafeList<uint> indices, float error)
    {
        fixed (uint* pIndices = new uint[(int)indices.Length])
        {
            for (int i = 0; i < (int)indices.Length; i++)
            {
                pIndices[i] = indices[i];
            }

            var bounds = Api.meshopt_computeClusterBounds(pIndices, (nuint)indices.Length, mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);
            
            var result = new ClodBounds();
            result.center[0] = bounds.center[0];
            result.center[1] = bounds.center[1];
            result.center[2] = bounds.center[2];
            result.radius = bounds.radius;
            result.error = error;
            return result;
        }
    }

    public static ClodBounds MergeBounds(UnsafeList<Cluster> clusters, UnsafeList<int> group)
    {
        var boundsList = new ClodBounds[group.Length];
        for (int j = 0; j < (int)group.Length; j++)
        {
            boundsList[j] = clusters[group[j]].bounds;
        }

        fixed (ClodBounds* pBounds = boundsList)
        {
            var merged = Api.meshopt_computeSphereBounds(
                &pBounds[0].center[0],
                (nuint)boundsList.Length,
                (nuint)sizeof(ClodBounds),
                &pBounds[0].radius,
                (nuint)sizeof(ClodBounds)
            );

            var result = new ClodBounds();
            result.center[0] = merged.center[0];
            result.center[1] = merged.center[1];
            result.center[2] = merged.center[2];
            result.radius = merged.radius;

            result.error = 0.0f;
            for (int j = 0; j < (int)group.Length; j++)
            {
                result.error = Math.Max(result.error, clusters[group[j]].bounds.error);
            }

            return result;
        }
    }
}
