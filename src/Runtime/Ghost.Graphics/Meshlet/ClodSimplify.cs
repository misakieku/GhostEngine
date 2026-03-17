using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Meshlet;

internal static class ClodSimplify
{
    public static unsafe UnsafeList<uint> Simplify(
        ClodConfig config,
        ClodMesh mesh,
        UnsafeList<uint> indices,
        UnsafeList<byte> locks,
        nuint targetCount,
        float* error
    )
    {
        if (targetCount >= (nuint)indices.Count)
            return indices;

        var lod = new UnsafeList<uint>(indices.Count, Allocator.Temp);
        lod.Resize(indices.Count);

        uint options = (uint)(Api.meshopt_SimplifySparse | Api.meshopt_SimplifyErrorAbsolute);
        if (config.simplifyPermissive)
            options |= (uint)Api.meshopt_SimplifyPermissive;
        if (config.simplifyRegularize)
            options |= (uint)Api.meshopt_SimplifyRegularize;

        nuint resultSize = MeshOptApi.SimplifyWithAttributes(
            (uint*)lod.GetUnsafePtr(),
            (uint*)indices.GetUnsafePtr(),
            (nuint)indices.Count,
            mesh.vertexPositions,
            mesh.vertexCount,
            mesh.vertexPositionsStride,
            mesh.vertexAttributes,
            mesh.vertexAttributesStride,
            mesh.attributeWeights,
            mesh.attributeCount,
            (byte*)locks.GetUnsafePtr(),
            targetCount,
            float.MaxValue,
            options,
            error
        );
        lod.Resize((int)resultSize);

        if ((nuint)lod.Count > targetCount && config.simplifyFallbackPermissive && !config.simplifyPermissive)
        {
            options |= (uint)Api.meshopt_SimplifyPermissive;
            resultSize = MeshOptApi.SimplifyWithAttributes(
                (uint*)lod.GetUnsafePtr(),
                (uint*)indices.GetUnsafePtr(),
                (nuint)indices.Count,
                mesh.vertexPositions,
                mesh.vertexCount,
                mesh.vertexPositionsStride,
                mesh.vertexAttributes,
                mesh.vertexAttributesStride,
                mesh.attributeWeights,
                mesh.attributeCount,
                (byte*)locks.GetUnsafePtr(),
                targetCount,
                float.MaxValue,
                options,
                error
            );
            lod.Resize((int)resultSize);
        }

        if ((nuint)lod.Count > targetCount && config.simplifyFallbackSloppy)
        {
            *error *= config.simplifyErrorFactorSloppy;
        }

        if (config.simplifyErrorEdgeLimit > 0)
        {
            float maxEdgeSq = 0;
            uint* pIdx = (uint*)indices.GetUnsafePtr();
            int posStride = (int)(mesh.vertexPositionsStride / sizeof(float));

            for (int i = 0; i < indices.Count; i += 3)
            {
                uint a = pIdx[i], b = pIdx[i + 1], c = pIdx[i + 2];
                float* va = mesh.vertexPositions + (a * (uint)posStride);
                float* vb = mesh.vertexPositions + (b * (uint)posStride);
                float* vc = mesh.vertexPositions + (c * (uint)posStride);

                float dx, dy, dz;
                dx = va[0] - vb[0]; dy = va[1] - vb[1]; dz = va[2] - vb[2];
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
}
