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
        float* error
    )
    {
        if (targetCount > (nuint)indices.Length)
        {
            return indices;
        }

        // Use Allocator.Temp for LOD results to avoid stack overflow on mega-meshes
        var lod = new UnsafeList<uint>((nuint)indices.Length, Allocator.Temp);
        lod.Resize((nuint)indices.Length);

        uint options = MeshOptApi.SimplifySparse | MeshOptApi.SimplifyErrorAbsolute;
        if (config.simplifyPermissive)
            options |= MeshOptApi.SimplifyPermissive;
        if (config.simplifyRegularize)
            options |= MeshOptApi.SimplifyRegularize;

        nuint resultSize = MeshOptApi.SimplifyWithAttributes(
            lod.GetUnsafePtr(),
            indices.GetUnsafePtr(),
            (nuint)indices.Length,
            mesh.vertexPositions,
            mesh.vertexCount,
            mesh.vertexPositionsStride,
            mesh.vertexAttributes,
            mesh.vertexAttributesStride,
            mesh.attributeWeights,
            mesh.attributeCount,
            locks.GetUnsafePtr(),
            targetCount,
            float.MaxValue,
            options,
            error
        );

        lod.Resize(resultSize);

        // Fallback to permissive if needed
        if (lod.Length > targetCount && config.simplifyFallbackPermissive && !config.simplifyPermissive)
        {
            options |= MeshOptApi.SimplifyPermissive;
            resultSize = MeshOptApi.SimplifyWithAttributes(
                lod.GetUnsafePtr(),
                indices.GetUnsafePtr(),
                (nuint)indices.Length,
                mesh.vertexPositions,
                mesh.vertexCount,
                mesh.vertexPositionsStride,
                mesh.vertexAttributes,
                mesh.vertexAttributesStride,
                mesh.attributeWeights,
                mesh.attributeCount,
                locks.GetUnsafePtr(),
                targetCount,
                float.MaxValue,
                options,
                error
            );
            lod.Resize(resultSize);
        }

        // Sloppy fallback
        if (lod.Length > targetCount && config.simplifyFallbackSloppy)
        {
            SimplifyFallback(lod, mesh, indices, locks, targetCount, error);
            *error *= config.simplifyErrorFactorSloppy;
        }

        // Edge limit check
        if (config.simplifyErrorEdgeLimit > 0)
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

            *error = Math.Min(*error, (float)Math.Sqrt(maxEdgeSq) * config.simplifyErrorEdgeLimit);
        }

        return lod;
    }

    private static void SimplifyFallback(
        UnsafeList<uint> lod,
        ClodMesh mesh,
        UnsafeList<uint> indices,
        UnsafeList<byte> locks,
        nuint targetCount,
        float* error
    )
    {
        // Placeholder for sloppy simplification fallback logic
    }
}
