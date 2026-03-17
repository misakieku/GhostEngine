using System;
using System.Numerics;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal static class ClodBoundsHelper
{
    public static ClodBounds ComputeBounds(ClodMesh mesh, UnsafeList<uint> indices, float error)
    {
        var bounds = MeshOptApi.ComputeClusterBounds(indices.GetUnsafePtr(), (nuint)indices.Length, mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);
        
        var result = new ClodBounds();
        result.center = new Vector3(bounds.center[0], bounds.center[1], bounds.center[2]);
        result.radius = bounds.radius;
        result.error = error;
        return result;
    }

    public static ClodBounds MergeBounds(UnsafeList<Cluster> clusters, UnsafeList<int> group)
    {
        using var scope = AllocationManager.CreateStackScope();
        var boundsList = new UnsafeList<ClodBounds>(group.Length, scope.AllocationHandle);
        boundsList.Resize((nuint)group.Length);
        
        for (int j = 0; j < (int)group.Length; j++)
        {
            boundsList[j] = clusters[group[j]].bounds;
        }

        var merged = MeshOptApi.ComputeSphereBounds(
            (float*)boundsList.GetUnsafePtr(),
            (nuint)group.Length,
            (nuint)sizeof(ClodBounds),
            (float*)boundsList.GetUnsafePtr() + 3, // offset to radius field
            (nuint)sizeof(ClodBounds)
        );

        var result = new ClodBounds();
        result.center = new Vector3(merged.center[0], merged.center[1], merged.center[2]);
        result.radius = merged.radius;

        result.error = 0.0f;
        for (int j = 0; j < (int)group.Length; j++)
        {
            result.error = Math.Max(result.error, clusters[group[j]].bounds.error);
        }

        return result;
    }
}
