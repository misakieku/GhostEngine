using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal static class ClodSimplify
{
    public static UnsafeList<uint> Simplify(
        ClodConfig config,
        ClodMesh mesh,
        UnsafeList<uint> indices,
        UnsafeList<byte> locks,
        nuint targetCount,
        float* error,
        Allocator allocator
    )
    {
        if (targetCount > (nuint)indices.Length)
        {
            return indices;
        }

        var lod = new UnsafeList<uint>(indices.Length, allocator);
        lod.Resize((nuint)indices.Length);

        uint options = Api.meshopt_SimplifySparse | Api.meshopt_SimplifyErrorAbsolute;
        if (config.SimplifyPermissive)
            options |= Api.meshopt_SimplifyPermissive;
        if (config.SimplifyRegularize)
            options |= Api.meshopt_SimplifyRegularize;

        fixed (uint* pIndices = new uint[(int)indices.Length])
        {
            fixed (byte* pLocks = new byte[(int)locks.Length])
            {
                for (int i = 0; i < (int)indices.Length; i++)
                    pIndices[i] = indices[i];
                for (int i = 0; i < (int)locks.Length; i++)
                    pLocks[i] = locks[i];

                nuint resultSize = Api.meshopt_simplifyWithAttributes(
                    lod.Ptr,
                    pIndices,
                    (nuint)indices.Length,
                    mesh.vertexPositions,
                    mesh.vertexCount,
                    mesh.vertexPositionsStride,
                    mesh.vertexAttributes,
                    mesh.vertexAttributesStride,
                    mesh.attributeWeights,
                    mesh.attributeCount,
                    pLocks,
                    targetCount,
                    float.MaxValue,
                    options,
                    error
                );

                lod.Resize(resultSize);

                // Fallback to permissive if needed
                if (lod.Length > targetCount && config.SimplifyFallbackPermissive && !config.SimplifyPermissive)
                {
                    options |= Api.meshopt_SimplifyPermissive;
                    resultSize = Api.meshopt_simplifyWithAttributes(
                        lod.Ptr,
                        pIndices,
                        (nuint)indices.Length,
                        mesh.vertexPositions,
                        mesh.vertexCount,
                        mesh.vertexPositionsStride,
                        mesh.vertexAttributes,
                        mesh.vertexAttributesStride,
                        mesh.attributeWeights,
                        mesh.attributeCount,
                        pLocks,
                        targetCount,
                        float.MaxValue,
                        options,
                        error
                    );
                    lod.Resize(resultSize);
                }

                // Sloppy fallback
                if (lod.Length > targetCount && config.SimplifyFallbackSloppy)
                {
                    SimplifyFallback(lod, mesh, indices, locks, targetCount, error, allocator);
                    *error *= config.SimplifyErrorFactorSloppy;
                }

                // Edge limit check
                if (config.SimplifyErrorEdgeLimit > 0)
                {
                    float maxEdgeSq = 0;
                    for (int i = 0; i < (int)indices.Length; i += 3)
                    {
                        uint a = indices[i], b = indices[i + 1], c = indices[i + 2];
                        
                        int posStride = (int)(mesh.vertexPositionsStride / sizeof(float));
                        float* va = mesh.vertexPositions + (a * posStride);
                        float* vb = mesh.vertexPositions + (b * posStride);
                        float* vc = mesh.vertexPositions + (c * posStride);

                        float dx = va[0] - vb[0], dy = va[1] - vb[1], dz = va[2] - vb[2];
                        float eab = dx * dx + dy * dy + dz * dz;
                        
                        dx = va[0] - vc[0]; dy = va[1] - vc[1]; dz = va[2] - vc[2];
                        float eac = dx * dx + dy * dy + dz * dz;
                        
                        dx = vb[0] - vc[0]; dy = vb[1] - vc[1]; dz = vb[2] - vc[2];
                        float ebc = dx * dx + dy * dy + dz * dz;

                        float emax = Math.Max(Math.Max(eab, eac), ebc);
                        float emin = Math.Min(Math.Min(eab, eac), ebc);

                        maxEdgeSq = Math.Max(maxEdgeSq, Math.Max(emin, emax / 4));
                    }

                    *error = Math.Min(*error, (float)Math.Sqrt(maxEdgeSq) * config.SimplifyErrorEdgeLimit);
                }
            }
        }

        return lod;
    }

    private static void SimplifyFallback(
        UnsafeList<uint> lod,
        ClodMesh mesh,
        UnsafeList<uint> indices,
        UnsafeList<byte> locks,
        nuint targetCount,
        float* error,
        Allocator allocator
    )
    {
        // Simplified version - deindex and use sloppy simplification
        // Implementation details would involve creating a subset for sparse simplification
        // For now, this is a placeholder
    }
}
