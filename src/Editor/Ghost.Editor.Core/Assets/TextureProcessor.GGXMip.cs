using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
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

        // Maps a 3D direction vector to 2D equirectangular UVs
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2<TFloat, float> DirToEquirectangularUV(Vector3<TFloat, float> dir)
        {
            var u = TFloat.Atan2(dir.z, dir.x);
            var v = TFloat.Asin(dir.y);

            u = u / (2.0f * PI) + 0.5f;
            v = v / PI + 0.5f;
            return MathV.Create<TFloat, float>(u, v);
        }

        // Samples the source HDR image using bilinear interpolation (simplified to nearest neighbor for brevity here)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3<TFloat, float> SampleEquirectangularMap(float* img, int w, int h, int c, Vector3<TFloat, float> dir)
        {
            var uv = DirToEquirectangularUV(dir);

            // Nearest neighbor pixel coordinates
            var px = (uv.x * (w - 1.0f)).Cast<TInt, int>();
            var py = (uv.y * (h - 1.0f)).Cast<TInt, int>();

            // Clamp
            px = TInt.Clamp(px, TInt.Zero, w - 1);
            py = TInt.Clamp(py, TInt.Zero, h - 1);

            // Assuming float RGB array format
            var idx = (py * w + px) * c;
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
            var h = pLevel->height;
            var pData = pLevel->data;

            var local_i = loopIndex - pLevel->offset;
            var x = local_i % w;
            var y = local_i / w;
            var u = (float)x / (w - 1);
            var v = (float)y / (h - 1);

            var phi = (u - 0.5f) * 2.0f * PI;
            var theta = (v - 0.5f) * PI;

            sincos(theta, out var sinTheta, out var cosTheta);
            sincos(phi, out var sinPhi, out var cosPhi);
            var N = float3(cosTheta * cosPhi, sinTheta, cosTheta * sinPhi);
            N = normalize(N);

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
                var sampleColor = SampleEquirectangularMap(pImage, imageWidth, imageHeight, channelCount, L);

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
            var out_idx = (y * w + x) * channelCount;
            pData[out_idx] = prefilteredColor.x;
            pData[out_idx + 1] = prefilteredColor.y;
            pData[out_idx + 2] = prefilteredColor.z;
            if (channelCount == 4)
            {
                pData[out_idx + 3] = 1.0f;
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

    private static JobHandle GenerateMipHDRI(JobScheduler scheduler, TextureAssetHandler.TextureInfo textureInfo, int totalMipLevels, out UnsafeArray<MipLevel> mipLevels)
    {
        Logger.DebugAssert(textureInfo.isHDR, "GenerateMipHDRI should only be called for HDR textures.");
        Logger.DebugAssert(textureInfo.colorComponents >= 3, "Texture must have at least 3 color components for RGB.");

        mipLevels = new UnsafeArray<MipLevel>(totalMipLevels, AllocationHandle.FreeList);
        var radicalInverse_VdCLut = new UnsafeArray<float>(_SAMPLE_COUNT, AllocationHandle.FreeList);

        for (var i = 0u; i < _SAMPLE_COUNT; i++)
        {
            radicalInverse_VdCLut[i] = RadicalInverse_VdC(i);
        }

        int w, h;
        var totalPixel = 0;

        for (var i = 0; i < totalMipLevels; i++)
        {
            w = Math.Max(1, textureInfo.width >> i);
            h = Math.Max(1, textureInfo.height >> i);

            mipLevels[i] = new MipLevel
            {
                data = new UnsafeArray<float>(w * h * textureInfo.colorComponents, AllocationHandle.FreeList),
                width = w,
                height = h,
                offset = totalPixel,
                roughness = (float)i / (totalMipLevels - 1) // Linear roughness from 0 to 1 across mip levels
            };

            totalPixel += w * h;
        }

        JobHandle handle;
        unsafe
        {
            if (WideLane.IsSupported)
            {
                var job = new GGXMipGenerationJobSPMD<WideLane<float>, WideLane<int>>
                {
                    pImage = (float*)textureInfo.pixelData,
                    pMipLevels = (MipLevel*)mipLevels.GetUnsafePtr(),
                    pRadicalInverse_VdCLut = (float*)radicalInverse_VdCLut.GetUnsafePtr(),
                    imageWidth = textureInfo.width,
                    imageHeight = textureInfo.height,
                    numMipLevels = totalMipLevels,
                    channelCount = textureInfo.colorComponents,
                };

                handle = scheduler.ScheduleParallelFor(in job, totalPixel, 64);
            }
            else
            {
                var job = new GGXMipGenerationJobSPMD<ScalarLane<float>, ScalarLane<int>>
                {
                    pImage = (float*)textureInfo.pixelData,
                    pMipLevels = (MipLevel*)mipLevels.GetUnsafePtr(),
                    pRadicalInverse_VdCLut = (float*)radicalInverse_VdCLut.GetUnsafePtr(),
                    imageWidth = textureInfo.width,
                    imageHeight = textureInfo.height,
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
