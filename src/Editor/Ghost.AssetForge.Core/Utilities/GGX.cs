using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.CompilerServices;

namespace Ghost.AssetForge.Core.Utilities;

internal static class GGX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadicalInverse_VdC(uint bits)
    {
        bits = (bits << 16) | (bits >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f; // bits / 0x100000000
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float RadicalInverse_VdC_Fast(uint bits)
    {
        return math.asfloat(0x3f800000u | (math.reversebits(bits) >> 9)) - 1.0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe Vector2<TFloat, float> Hammersley<TFloat>(TFloat i, int N, float* lut)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var x = i / N;
        var y = TFloat.Load(lut + (int)i[0]); // Ensure index is properly mapped per lane if TFloat.Load supports it. Actually the original code did: TFloat.Load(lut + (int)i[0]);
        return MathV.Create<TFloat, float>(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe Vector2<TFloat, float> Hammersley<TFloat>(TFloat i, int N)
    where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var x = i / N;
        var y = TFloat.Zero;

        for (var lane = 0; lane < TFloat.LaneWidth; lane++)
        {
            var index = (uint)i[lane];
            y.GetUnsafePtr()[lane] = RadicalInverse_VdC(index);
        }

        return MathV.Create<TFloat, float>(x, y);
    }

    // GGX Importance Sampling
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector3<TFloat, float> ImportanceSampleGGX<TFloat>(Vector2<TFloat, float> Xi, Vector3<TFloat, float> N, float roughness)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var a = roughness * roughness; // Disney remap roughness for better visual linearity

        var phi = 2.0f * MathF.PI * Xi.x;

        var cosTheta = TFloat.Sqrt((1.0f - Xi.y) / (1.0f + (a * a - 1.0f) * Xi.y));
        var sinTheta = TFloat.Sqrt(TFloat.Max(TFloat.Zero, 1.0f - cosTheta * cosTheta));

        // Spherical to Cartesian coordinates (Halfway vector)
        TFloat.SinCos(phi, out var sinPhi, out var cosPhi);
        var H = MathV.Create<TFloat, float>(cosPhi * sinTheta, sinPhi * sinTheta, cosTheta);

        // // Tangent space to World space
        // var mask = TFloat.Abs(N.z) < 0.999f;
        // var up = MathV.Select(mask, MathV.Create<TFloat, float>(0.0f, 0.0f, 1.0f), MathV.Create<TFloat, float>(1.0f, 0.0f, 0.0f));
        // 
        // var tangent = MathV.Normalize(MathV.Cross(up, N));
        // var bitangent = MathV.Cross(N, tangent);
        // 
        // var sampleVec = (tangent * H.x) + (bitangent * H.y) + (N * H.z);
        return MathV.Normalize(H);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static TFloat GeometrySchlickGGX<TFloat>(TFloat NdotV, TFloat roughness)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var a = roughness;
        var k = (a * a) / 2.0f;

        var nom = NdotV;
        var denom = NdotV * (1.0f - k) + k;

        return nom / denom;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static TFloat GeometrySmith<TFloat>(TFloat roughness, TFloat NoV, TFloat NoL)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var ggx2 = GeometrySchlickGGX(NoV, roughness);
        var ggx1 = GeometrySchlickGGX(NoL, roughness);

        return ggx1 * ggx2;
    }
}
