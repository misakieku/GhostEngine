using System;
using System.Numerics;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Meshlet;

internal static class ClodBoundsHelper
{
    public static unsafe ClodBounds ComputeBounds(ClodMesh mesh, UnsafeList<uint> indices, float error)
    {
        var bounds = MeshOptApi.ComputeClusterBounds((uint*)indices.GetUnsafePtr(), (nuint)indices.Count, mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);
        return new ClodBounds
        {
            center = new Vector3(bounds.center[0], bounds.center[1], bounds.center[2]),
            radius = bounds.radius,
            error = error
        };
    }

    public static unsafe ClodBounds MergeBounds(UnsafeList<Cluster> clusters, UnsafeList<int> group)
    {
        var boundsList = new UnsafeList<ClodBounds>(group.Count, Allocator.Temp);
        for (int j = 0; j < group.Count; j++)
            boundsList.Add(clusters[group[j]].bounds);

        var merged = MeshOptApi.ComputeSphereBounds(
            (float*)boundsList.GetUnsafePtr(),
            (nuint)group.Count,
            (nuint)sizeof(ClodBounds),
            (float*)boundsList.GetUnsafePtr() + 3,
            (nuint)sizeof(ClodBounds)
        );

        float maxError = 0.0f;
        for (int j = 0; j < group.Count; j++)
            maxError = Math.Max(maxError, clusters[group[j]].bounds.error);

        boundsList.Dispose();
        return new ClodBounds
        {
            center = new Vector3(merged.center[0], merged.center[1], merged.center[2]),
            radius = merged.radius,
            error = maxError
        };
    }
}
