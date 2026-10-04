using Ghost.AssetForge.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;

namespace Ghost.AssetForge.Core.Generator;

internal unsafe class PbrLutGenerator
{
    private const int SAMPLE_COUNT = 256;

    private static float2 IntegrateBRDF<TFloat>(float ndv, float roughness, float* lut)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var V = MathV.Create<TFloat, float>(math.sqrt(math.max(0.0f, 1.0f - ndv * ndv)), 0.0f, ndv);
        var N = MathV.Create<TFloat, float>(0.0f, 0.0f, 1.0f);

        var vRoughness = TFloat.Create(roughness);
        var NdotV = TFloat.Create(ndv);

        var a = TFloat.Zero;
        var b = TFloat.Zero;

        for (var j = 0; j < SAMPLE_COUNT; j += TFloat.LaneWidth)
        {
            var laneIndices = TFloat.Sequence(j, 1.0f);
            // Should be safe to not use mask.
            // var validLaneMask = laneIndices < SAMPLE_COUNT;

            var Xi = GGX.Hammersley(laneIndices, SAMPLE_COUNT, lut);
            var H = GGX.ImportanceSampleGGX(Xi, N, roughness);
            var L = MathV.Normalize(MathV.Reflect(-V, H));

            var NdotL = TFloat.Saturate(L.z);
            var NdotH = TFloat.Saturate(H.z);
            var VdotH = TFloat.Saturate(MathV.Dot(V, H));

            var mask = NdotL > 0.0f;

            var G = GGX.GeometrySmith(vRoughness, NdotV, NdotL);
            var G_Vis = G * VdotH / TFloat.Max(NdotH * NdotV, 1e-5f);
            var Fc = TFloat.Pow(1.0f - VdotH, 5.0f);

            a += TFloat.Select(mask, (1.0f - Fc) * G_Vis, 0.0f);
            b += TFloat.Select(mask, Fc * G_Vis, 0.0f);
        }

        var x = 0.0f;
        var y = 0.0f;
        for (var lane = 0; lane < TFloat.LaneWidth; lane++)
        {
            x += a[lane];
            y += b[lane];
        }

        return new float2(x, y) / SAMPLE_COUNT;
    }

    public static Task GenerateAsync(int size, string output, CancellationToken cancellationToken)
    {
        var rcpSize = 1.0f / size;

        using var lut = new UnsafeArray<float2>(size * size, AllocationHandle.TLSF);
        using var radicalInverse_VdCLut = new UnsafeArray<float>(SAMPLE_COUNT, AllocationHandle.TLSF);

        for (var i = 0u; i < SAMPLE_COUNT; i++)
        {
            radicalInverse_VdCLut[i] = GGX.RadicalInverse_VdC(i);
        }

        if (WideLane.IsSupported)
        {
            Parallel.For(0, lut.Length, i =>
            {
                var px = i % size;
                var py = i / size;
                var ndv = (px + 0.5f) * rcpSize;
                var roughness = (py + 0.5f) * rcpSize;

                lut[i] = IntegrateBRDF<WideLane<float>>(ndv, roughness, (float*)radicalInverse_VdCLut.GetUnsafePtr());
            });
        }
        else
        {
            Parallel.For(0, lut.Length, i =>
            {
                var px = i % size;
                var py = i / size;
                var ndv = (px + 0.5f) * rcpSize;
                var roughness = (py + 0.5f) * rcpSize;

                lut[i] = IntegrateBRDF<ScalarLane<float>>(ndv, roughness, (float*)radicalInverse_VdCLut.GetUnsafePtr());
            });
        }

        using var rg16Lut = new UnsafeArray<Half>(size * size * 2, AllocationHandle.TLSF);

        for (var i = 0; i < lut.Length; i++)
        {
            rg16Lut[i * 2] = (Half)lut[i].x;
            rg16Lut[i * 2 + 1] = (Half)lut[i].y;
        }

        DDSUtility.WriteDdsR16G16F(output, size, size, rg16Lut);

        return Task.CompletedTask;
    }
}
