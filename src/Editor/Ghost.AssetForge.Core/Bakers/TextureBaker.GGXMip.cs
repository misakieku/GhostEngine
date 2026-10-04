using Ghost.AssetForge.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.CompilerServices;
using static Misaki.HighPerformance.Mathematics.math;

namespace Ghost.AssetForge.Core.Bakers;

internal partial class TextureBaker
{
    private const int SAMPLE_COUNT = 1024;

    private struct MipLevel
    {
        public UnsafeArray<float> data;
        public int width;
        public int height;
        public int offset;
        public float roughness;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static float3 CubemapUVToDir(int face, float u, float v)
    {
        var sc = 2.0f * u - 1.0f;
        var tc = 1.0f - 2.0f * v;

        float x = 0.0f, y = 0.0f, z = 0.0f;
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

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe Vector3<TFloat, float> SampleCubemap<TFloat, TInt>(float* img, int edge, int c, Vector3<TFloat, float> dir)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
        where TInt : unmanaged, ISPMDLane<TInt, int>
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static unsafe void ProcessGGXMipPixel<TFloat, TInt>(int loopIndex, float* pImage, MipLevel* pMipLevels, float* pRadicalInverse_VdCLut, int imageWidth, int imageHeight, int numMipLevels, int channelCount)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
        where TInt : unmanaged, ISPMDLane<TInt, int>
    {
        var m = 0;
        while (m < numMipLevels - 1 && loopIndex >= pMipLevels[m + 1].offset)
        {
            m++;
        }

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
        var dynamicSampleCount = (int)max(1.0f, SAMPLE_COUNT * pLevel->roughness);
        var dsc = TFloat.Create(dynamicSampleCount);

        for (var i = 0; i < dynamicSampleCount; i += TFloat.LaneWidth)
        {
            var laneIndices = TFloat.Sequence(i, 1.0f);
            var validLaneMask = laneIndices < dsc;

            // Generate a Hammersley random sequence point
            var Xi = GGX.Hammersley(laneIndices, dynamicSampleCount, pRadicalInverse_VdCLut);

            // Get the halfway vector based on GGX NDF
            var H = GGX.ImportanceSampleGGX(Xi, vN, pLevel->roughness);

            // Calculate Light direction
            var L = MathV.Reflect(-vV, H);
            L = MathV.Normalize(L);

            var NdotL = TFloat.Max(MathV.Dot(vN, L), TFloat.Zero);
            var sampleColor = SampleCubemap<TFloat, TInt>(pImage, imageWidth, channelCount, L);

            NdotL &= validLaneMask;

            // Denoising
            var luma = MathV.Dot(sampleColor, vLuma);
            var fireflyWeight = TFloat.One / (TFloat.One + luma);
            var finalWeight = NdotL * fireflyWeight;

            vPrefilteredColor += sampleColor * finalWeight;
            vTotalWeight += finalWeight;
        }

        var totalWeight = 0.0f;
        var prefilteredColor = float3(0.0f, 0.0f, 0.0f);

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

    private static UnsafeArray<MipLevel> GenerateMipHDRI(TextureInfo textureInfo, UnsafeArray<float> baseCubeData, int edge, int totalMipLevels)
    {
        System.Diagnostics.Debug.Assert(textureInfo.isCube, "GenerateMipHDRI should only be called for cube maps.");
        System.Diagnostics.Debug.Assert(textureInfo.colorComponents >= 3, "Texture must have at least 3 color components for RGB.");

        var mipLevels = new UnsafeArray<MipLevel>(totalMipLevels, AllocationHandle.TLSF);
        using var radicalInverse_VdCLut = new UnsafeArray<float>(SAMPLE_COUNT, AllocationHandle.TLSF);

        for (var i = 0u; i < SAMPLE_COUNT; i++)
        {
            radicalInverse_VdCLut[i] = GGX.RadicalInverse_VdC(i);
        }

        int w;
        var totalPixel = 0;

        for (var i = 0; i < totalMipLevels; i++)
        {
            w = Math.Max(1, edge >> i);

            mipLevels[i] = new MipLevel
            {
                data = new UnsafeArray<float>(w * w * 6 * textureInfo.colorComponents, AllocationHandle.TLSF),
                width = w,
                height = w,
                offset = totalPixel,
                roughness = (float)i / (totalMipLevels - 1) // Linear roughness from 0 to 1 across mip levels
            };

            totalPixel += w * w * 6;
        }

        unsafe
        {
            var pImage = (float*)baseCubeData.GetUnsafePtr();
            var pMipLevels = (MipLevel*)mipLevels.GetUnsafePtr();
            var pRadicalInverse_VdCLut = (float*)radicalInverse_VdCLut.GetUnsafePtr();
            var imageWidth = edge;
            var imageHeight = edge;
            var numMipLevels = totalMipLevels;
            var channelCount = textureInfo.colorComponents;

            if (WideLane.IsSupported)
            {
                Parallel.For(0, totalPixel, loopIndex =>
                {
                    ProcessGGXMipPixel<WideLane<float>, WideLane<int>>(
                        loopIndex,
                        pImage,
                        pMipLevels,
                        pRadicalInverse_VdCLut,
                        imageWidth,
                        imageHeight,
                        numMipLevels,
                        channelCount);
                });
            }
            else
            {
                Parallel.For(0, totalPixel, loopIndex =>
                {
                    ProcessGGXMipPixel<ScalarLane<float>, ScalarLane<int>>(
                        loopIndex,
                        pImage,
                        pMipLevels,
                        pRadicalInverse_VdCLut,
                        imageWidth,
                        imageHeight,
                        numMipLevels,
                        channelCount);
                });
            }
        }

        return mipLevels;
    }
}
