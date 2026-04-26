using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.CompilerServices;
using static Misaki.HighPerformance.Mathematics.math;

namespace Ghost.Editor.Core.Assets;

internal static partial class TextureProcessor
{
    private const int _SAMPLE_COUNT = 1024;

    private struct MipLevel
    {
        public UnsafeArray<float> data;
        public int width;
        public int height;
        public int offset;
        public float roughness;
    }

    private unsafe struct GGXMipGenerationJobSPMD<TFloat, TInt> : IJobParallelFor
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
        where TInt : unmanaged, ISPMDLane<TInt, int>
    {
        public float* pImage;
        public MipLevel* pMipLevels;
        public float* pRadicalInverse_VdCLut;
        public int imageWidth;
        public int imageHeight;
        public int numMipLevels;
        public int channelCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2<TFloat, float> Hammersley(TFloat i, int N, float* lut)
        {
            var x = i / N;
            var y = TFloat.Load(lut + (int)i[0]);
            return MathV.Create<TFloat, float>(x, y);
        }

        // GGX Importance Sampling
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3<TFloat, float> ImportanceSampleGGX(Vector2<TFloat, float> Xi, Vector3<TFloat, float> N, float roughness)
        {
            var a = roughness * roughness; // Disney remap roughness for better visual linearity

            var phi = 2.0f * PI * Xi.x;

            // Clamp the inside of the cosTheta Sqrt to prevent NaN on division precision edges
            var cosThetaInner = TFloat.Max((1.0f - Xi.y) / (1.0f + (a * a - 1.0f) * Xi.y), TFloat.Zero);
            var cosTheta = TFloat.Sqrt(cosThetaInner);

            // Clamp the inside of sinTheta to prevent sqrt of negative floating-point errors
            var sinThetaInner = TFloat.Max(1.0f - cosTheta * cosTheta, TFloat.Zero);
            var sinTheta = TFloat.Sqrt(sinThetaInner);

            // Spherical to Cartesian coordinates (Halfway vector)
            var (sinPhi, cosPhi) = TFloat.SinCos(phi);
            var H = MathV.Create<TFloat, float>(cosPhi * sinTheta, sinPhi * sinTheta, cosTheta);

            // Tangent space to World space
            var mask = TFloat.Abs(N.z) < 0.999f;
            var up = MathV.Select(mask, MathV.Create<TFloat, float>(0.0f, 0.0f, 1.0f), MathV.Create<TFloat, float>(1.0f, 0.0f, 0.0f));

            var tangent = MathV.Normalize(MathV.Cross(up, N));
            var bitangent = MathV.Cross(N, tangent);

            var sampleVec = (tangent * H.x) + (bitangent * H.y) + (N * H.z);
            return MathV.Normalize(sampleVec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 CubemapUVToDir(int face, float u, float v)
        {
            var sc = 2.0f * u - 1.0f;
            var tc = 1.0f - 2.0f * v;

            float x = 0, y = 0, z = 0;
            switch (face)
            {
                case 0: x = 1.0f; y = tc; z = -sc; break;
                case 1: x = -1.0f; y = tc; z = sc; break;
                case 2: x = sc; y = 1.0f; z = -tc; break;
                case 3: x = sc; y = -1.0f; z = tc; break;
                case 4: x = sc; y = tc; z = 1.0f; break;
                case 5: x = -sc; y = tc; z = -1.0f; break;
            }

            return normalize(float3(x, y, z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3<TFloat, float> SampleCubemap(float* img, int edge, int c, Vector3<TFloat, float> dir)
        {
            var absX = TFloat.Abs(dir.x);
            var absY = TFloat.Abs(dir.y);
            var absZ = TFloat.Abs(dir.z);

            var isXPos = dir.x >= TFloat.Zero;
            var isYPos = dir.y >= TFloat.Zero;
            var isZPos = dir.z >= TFloat.Zero;

            var maxAxis = TFloat.Max(TFloat.Max(absX, absY), absZ);

            var faceIndexF = TFloat.Select(maxAxis == absX,
                TFloat.Select(isXPos, 0.0f, 1.0f),
                TFloat.Select(maxAxis == absY,
                    TFloat.Select(isYPos, 2.0f, 3.0f),
                    TFloat.Select(isZPos, 4.0f, 5.0f)));

            var faceIndex = faceIndexF.Cast<TInt, int>();

            var sc = TFloat.Select(maxAxis == absX,
                TFloat.Select(isXPos, -dir.z, dir.z),
                TFloat.Select(maxAxis == absY,
                    dir.x,
                    TFloat.Select(isZPos, dir.x, -dir.x)));

            var tc = TFloat.Select(maxAxis == absX,
                dir.y,
                TFloat.Select(maxAxis == absY,
                    TFloat.Select(isYPos, -dir.z, dir.z),
                    dir.y));

            var u = 0.5f * (sc / maxAxis + 1.0f);
            var v = 0.5f * (1.0f - tc / maxAxis);

            var px = (u * (edge - 1.0f)).Cast<TInt, int>();
            var py = (v * (edge - 1.0f)).Cast<TInt, int>();

            px = TInt.Clamp(px, TInt.Zero, edge - 1);
            py = TInt.Clamp(py, TInt.Zero, edge - 1);

            var faceOffset = faceIndex * (edge * edge);
            var idx = (faceOffset + py * edge + px) * c;
            return MathV.GatherVector3<TFloat, float>(img, idx.GetUnsafePtr(), 1);
        }

        public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
        {
            var m = 0;
            while (m < numMipLevels - 1 && loopIndex >= pMipLevels[m + 1].offset)
            {
                m++;
            }

            var span = new ReadOnlySpan<MipLevel>(pMipLevels, numMipLevels);
            var pLevel = &pMipLevels[m];

            var w = pLevel->width;
            var data = pLevel->data;

            var local_i = loopIndex - pLevel->offset;

            var faceArea = w * w;
            var face = local_i / faceArea;
            var face_local_i = local_i % faceArea;
            var x = face_local_i % w;
            var y = face_local_i / w;

            var u = (x + 0.5f) / w;
            var v = (y + 0.5f) / w;

            var N = CubemapUVToDir(face, u, v);

            // For split-sum, we assume View and Reflection directions equal the Normal
            var V = N;
            var R = N;

            var vN = MathV.Create<TFloat, float>(
                TFloat.Create(N.x),
                TFloat.Create(N.y),
                TFloat.Create(N.z)
            );

            var vV = MathV.Create<TFloat, float>(
                TFloat.Create(V.x),
                TFloat.Create(V.y),
                TFloat.Create(V.z)
            );

            var vPrefilteredColor = Vector3<TFloat, float>.Zero;
            var vTotalWeight = TFloat.Zero;

            // Monte Carlo Integration Loop

            var vLuma = MathV.Create<TFloat, float>(0.2126f, 0.7152f, 0.0722f);
            var dynamicSampleCount = (int)max(1.0f, _SAMPLE_COUNT * pLevel->roughness);
            var dsc = TFloat.Create(dynamicSampleCount);

            for (var i = 0; i < dynamicSampleCount; i += TFloat.LaneWidth)
            {
                var laneIndices = TFloat.Sequence(i, 1.0f);
                var validLaneMask = laneIndices < dsc;

                // Generate a Hammersley random sequence point
                var Xi = Hammersley(laneIndices, dynamicSampleCount, pRadicalInverse_VdCLut);

                // Get the halfway vector based on GGX NDF
                var H = ImportanceSampleGGX(Xi, vN, pLevel->roughness);

                // Calculate Light direction
                var L = MathV.Reflect(-vV, H);
                L = MathV.Normalize(L);

                var NdotL = TFloat.Max(MathV.Dot(vN, L), TFloat.Zero);
                var sampleColor = SampleCubemap(pImage, imageWidth, channelCount, L);

                NdotL &= validLaneMask;

                // The Karis Average Weight: 1 / (1 + luma)
                // A normal sky pixel (luma 1.0) gets a weight of 0.5.
                // A sun pixel (luma 1000.0) gets a tiny weight of ~0.001, naturally suppressing it.
                // This introduce bias, but significantly reduces fireflies without needing solid angle sampling or cdf inversion.
                // And since this is a mip generation step, a little bias is acceptable for much better performance and stability.
                var luma = MathV.Dot(sampleColor, vLuma);
                var fireflyWeight = TFloat.One / (TFloat.One + luma);
                var finalWeight = NdotL * fireflyWeight;

                vPrefilteredColor += sampleColor * finalWeight;
                vTotalWeight += finalWeight;
            }

            var totalWeight = 0.0f;
            var prefilteredColor = float3(0, 0, 0);

            for (var i = 0; i < TFloat.LaneWidth; i++)
            {
                prefilteredColor.x += vPrefilteredColor.x[i];
                prefilteredColor.y += vPrefilteredColor.y[i];
                prefilteredColor.z += vPrefilteredColor.z[i];
                totalWeight += vTotalWeight[i];
            }

            // Average the result
            if (totalWeight > 0.0f)
            {
                prefilteredColor *= 1.0f / totalWeight;
            }

            // Write to output mip array
            var out_idx = (face * (w * w) + y * w + x) * channelCount;
            data[out_idx] = prefilteredColor.x;
            data[out_idx + 1] = prefilteredColor.y;
            data[out_idx + 2] = prefilteredColor.z;
            if (channelCount == 4)
            {
                data[out_idx + 3] = 1.0f;
            }
        }
    }

    private struct VdCLutDisposeJob : IJob
    {
        public UnsafeArray<float> radicalInverse_VdCLut;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            radicalInverse_VdCLut.Dispose();
        }
    }

    private struct MipLevelDisposeJob : IJob
    {
        public UnsafeArray<MipLevel> mipLevels;

        public void Execute(ref readonly JobExecutionContext ctx)
        {
            for (var i = 0; i < mipLevels.Length; i++)
            {
                mipLevels[i].data.Dispose();
            }

            mipLevels.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float RadicalInverse_VdC(uint bits)
    {
        bits = (bits << 16) | (bits >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f; // bits / 0x100000000
    }

    private static JobHandle GenerateMipHDRI(JobScheduler scheduler, TextureAssetHandler.TextureInfo textureInfo, UnsafeArray<float> baseCubeData, int edge, int totalMipLevels, out UnsafeArray<MipLevel> mipLevels)
    {
        Logger.DebugAssert(textureInfo.isHDR, "GenerateMipHDRI should only be called for HDR textures.");
        Logger.DebugAssert(textureInfo.colorComponents >= 3, "Texture must have at least 3 color components for RGB.");

        mipLevels = new UnsafeArray<MipLevel>(totalMipLevels, AllocationHandle.FreeList);
        var radicalInverse_VdCLut = new UnsafeArray<float>(_SAMPLE_COUNT, AllocationHandle.FreeList);

        for (var i = 0u; i < _SAMPLE_COUNT; i++)
        {
            radicalInverse_VdCLut[i] = RadicalInverse_VdC(i);
        }

        int w;
        var totalPixel = 0;

        for (var i = 0; i < totalMipLevels; i++)
        {
            w = Math.Max(1, edge >> i);

            mipLevels[i] = new MipLevel
            {
                data = new UnsafeArray<float>(w * w * 6 * textureInfo.colorComponents, AllocationHandle.FreeList),
                width = w,
                height = w,
                offset = totalPixel,
                roughness = (float)i / (totalMipLevels - 1) // Linear roughness from 0 to 1 across mip levels
            };

            totalPixel += w * w * 6;
        }

        JobHandle handle;
        unsafe
        {
            if (WideLane.IsSupported)
            {
                var job = new GGXMipGenerationJobSPMD<WideLane<float>, WideLane<int>>
                {
                    pImage = (float*)baseCubeData.GetUnsafePtr(),
                    pMipLevels = (MipLevel*)mipLevels.GetUnsafePtr(),
                    pRadicalInverse_VdCLut = (float*)radicalInverse_VdCLut.GetUnsafePtr(),
                    imageWidth = edge,
                    imageHeight = edge,
                    numMipLevels = totalMipLevels,
                    channelCount = textureInfo.colorComponents,
                };

                handle = scheduler.ScheduleParallelFor(in job, totalPixel, 64);
            }
            else
            {
                var job = new GGXMipGenerationJobSPMD<ScalarLane<float>, ScalarLane<int>>
                {
                    pImage = (float*)baseCubeData.GetUnsafePtr(),
                    pMipLevels = (MipLevel*)mipLevels.GetUnsafePtr(),
                    pRadicalInverse_VdCLut = (float*)radicalInverse_VdCLut.GetUnsafePtr(),
                    imageWidth = edge,
                    imageHeight = edge,
                    numMipLevels = totalMipLevels,
                    channelCount = textureInfo.colorComponents,
                };

                handle = scheduler.ScheduleParallelFor(in job, totalPixel, 64);
            }
        }

        if (!handle.IsValid)
        {
            return JobHandle.Invalid;
        }

        var disposeJob = new VdCLutDisposeJob
        {
            radicalInverse_VdCLut = radicalInverse_VdCLut
        };

        var disposeHandle = scheduler.Schedule(in disposeJob, handle);
        Logger.DebugAssert(disposeHandle.IsValid, "Dispose job handle is invalid.");

        return disposeHandle;
    }
}
