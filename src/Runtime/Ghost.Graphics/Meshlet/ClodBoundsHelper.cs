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
        // Use Temp for the bounds list to support mega-meshes without stack overflow
        var boundsList = new UnsafeList<ClodBounds>((nuint)group.Length, Allocator.Temp);
        
        for (int j = 0; j < (int)group.Length; j++)
        {
            boundsList.Add(clusters[group[j]].bounds);
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

        boundsList.Dispose();
        return result;
    }
}
