using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Generator;

internal unsafe class StbnGenerator
{
    private const int SPATIAL_RADIUS = 4;
    private const int TEMPORAL_RADIUS = 3;
    private const int ITERATION_COUNT = 32;
    private const float SPATIAL_SIGMA = 1.6f;
    private const float TEMPORAL_SIGMA = 1.0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Hash01(uint x, uint y, uint z, uint channel)
    {
        var seed = x * 1664525u + y * 1013904223u + z * 374761393u + channel * 668265263u;
        seed = (seed ^ 61u) ^ (seed >> 16);
        seed *= 9u;
        seed ^= seed >> 4;
        seed *= 0x27d4eb2du;
        seed ^= seed >> 15;
        return (seed & 0x00FFFFFFu) * (1.0f / 16777216.0f);
    }

    private static void ConvolveHorizontal<TFloat>(float* pSrc, float* pDst, float* pKernel, int size, int depth)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var mask = size - 1;
        var laneWidth = TFloat.LaneWidth;

        Parallel.For(0, depth * size, rowIdx =>
        {
            var rowSrc = pSrc + rowIdx * size;
            var rowDst = pDst + rowIdx * size;
            var tempBuf = stackalloc float[laneWidth];

            for (var x = 0; x < size; x += laneWidth)
            {
                var sum = TFloat.Zero;
                var isInterior = (x >= SPATIAL_RADIUS) && (x + laneWidth + SPATIAL_RADIUS <= size);

                if (isInterior)
                {
                    for (var dx = -SPATIAL_RADIUS; dx <= SPATIAL_RADIUS; dx++)
                    {
                        var w = TFloat.Create(pKernel[dx + SPATIAL_RADIUS]);
                        var v = TFloat.Load(rowSrc + x + dx);
                        sum += v * w;
                    }
                }
                else
                {
                    for (var dx = -SPATIAL_RADIUS; dx <= SPATIAL_RADIUS; dx++)
                    {
                        var w = TFloat.Create(pKernel[dx + SPATIAL_RADIUS]);
                        for (var lane = 0; lane < laneWidth; lane++)
                        {
                            tempBuf[lane] = rowSrc[(x + lane + dx) & mask];
                        }

                        var v = TFloat.Load(tempBuf);
                        sum += v * w;
                    }
                }

                sum.Store(rowDst + x);
            }
        });
    }

    private static void ConvolveVertical<TFloat>(float* pSrc, float* pDst, float* pKernel, int size, int depth)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var sliceStride = size * size;
        var mask = size - 1;
        var laneWidth = TFloat.LaneWidth;

        Parallel.For(0, depth * size, rowIdx =>
        {
            var z = rowIdx / size;
            var y = rowIdx % size;
            var sliceSrc = pSrc + z * sliceStride;
            var rowDst = pDst + rowIdx * size;

            for (var x = 0; x < size; x += laneWidth)
            {
                var sum = TFloat.Zero;
                for (var dy = -SPATIAL_RADIUS; dy <= SPATIAL_RADIUS; dy++)
                {
                    var wy = pKernel[dy + SPATIAL_RADIUS];
                    var wrappedY = (y + dy) & mask;
                    var v = TFloat.Load(sliceSrc + wrappedY * size + x);
                    sum += v * wy;
                }

                sum.Store(rowDst + x);
            }
        });
    }

    private static void EvaluateHighPass<TFloat>(float* pCurrent, float* pSpatialBlur, float* pHighPass, float* pTemporalKernel, int size, int depth)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var sliceStride = size * size;
        var zMask = depth - 1;
        var laneWidth = TFloat.LaneWidth;
        var centerSpatialWeight = pTemporalKernel[TEMPORAL_RADIUS];
        var rcpNeighborSpatialWeight = 1.0f / (1.0f - centerSpatialWeight);

        Parallel.For(0, depth * size, rowIdx =>
        {
            var z = rowIdx / size;
            var y = rowIdx % size;
            var rowOffset = y * size;
            var dstRow = pHighPass + z * sliceStride + rowOffset;

            for (var x = 0; x < size; x += laneWidth)
            {
                var u = TFloat.Load(pCurrent + z * sliceStride + rowOffset + x);
                var spatialLowPass = TFloat.Load(pSpatialBlur + z * sliceStride + rowOffset + x);
                var neighborSpatialAvg = (spatialLowPass - u * centerSpatialWeight) * rcpNeighborSpatialWeight;

                var temporalLowPass = TFloat.Zero;
                var spatioTemporalLowPass = TFloat.Zero;

                for (var dz = -TEMPORAL_RADIUS; dz <= TEMPORAL_RADIUS; dz++)
                {
                    if (dz == 0)
                    {
                        continue;
                    }

                    var wz = pTemporalKernel[dz + TEMPORAL_RADIUS];
                    var wrappedZ = (z + dz) & zMask;
                    var neighborOffset = wrappedZ * sliceStride + rowOffset + x;

                    temporalLowPass += TFloat.Load(pCurrent + neighborOffset) * wz;
                    spatioTemporalLowPass += TFloat.Load(pSpatialBlur + neighborOffset) * wz;
                }

                var localMean = neighborSpatialAvg * 0.55f + temporalLowPass * 0.30f + spatioTemporalLowPass * 0.15f;
                var highPass = u - localMean;
                highPass.Store(dstRow + x);
            }
        });
    }

    private static void EqualizeTemporalRanks(float* pHighPass, int size, int depth)
    {
        var sliceStride = size * size;
        var rcpDepth = 1.0f / depth;

        Parallel.For(0, sliceStride, pixelIdx =>
        {
            Span<float> values = stackalloc float[depth];
            Span<int> indices = stackalloc int[depth];

            for (var z = 0; z < depth; z++)
            {
                values[z] = pHighPass[z * sliceStride + pixelIdx];
                indices[z] = z;
            }

            values.Sort(indices);

            for (var rank = 0; rank < depth; rank++)
            {
                var z = indices[rank];
                var stratified = (rank + 0.5f) * rcpDepth;
                pHighPass[z * sliceStride + pixelIdx] = math.lerp(pHighPass[z * sliceStride + pixelIdx], stratified, 0.45f);
            }
        });
    }

    private static void EqualizeSpatialRanks(float* pHighPass, float* pCurrent, float* pSortKeys, int* pSortIndices, int size, int depth, uint channel, int iteration)
    {
        var sliceStride = size * size;
        var rcpSliceCount = 1.0f / sliceStride;

        Parallel.For(0, depth, z =>
        {
            var sliceOffset = z * sliceStride;
            var sliceHighPass = pHighPass + sliceOffset;
            var sliceCurrent = pCurrent + sliceOffset;
            var sliceKeys = pSortKeys + sliceOffset;
            var sliceIndices = pSortIndices + sliceOffset;

            for (var i = 0; i < sliceStride; i++)
            {
                var jitter = (Hash01((uint)i, (uint)z, (uint)iteration, channel) - 0.5f) * 1e-6f;
                sliceKeys[i] = sliceHighPass[i] + jitter;
                sliceIndices[i] = i;
            }

            MemoryMarshal.CreateSpan(ref *sliceKeys, sliceStride).Sort(MemoryMarshal.CreateSpan(ref *sliceIndices, sliceStride));

            for (var rank = 0; rank < sliceStride; rank++)
            {
                var pixelIdx = sliceIndices[rank];
                sliceCurrent[pixelIdx] = (rank + 0.5f) * rcpSliceCount;
            }
        });
    }

    private static void GenerateChannel<TFloat>(float* pChannel, float* pTempX, float* pSpatialBlur, float* pHighPass, float* pSortKeys, int* pSortIndices, float* pSpatialKernel, float* pTemporalKernel, int size, int depth, uint channelSeed)
        where TFloat : unmanaged, ISPMDLane<TFloat, float>
    {
        var sliceStride = size * size;
        var goldenConjugate = (channelSeed == 0) ? 0.61803398875f : 0.75487766625f;

        Parallel.For(0, depth, z =>
        {
            var slicePtr = pChannel + z * sliceStride;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var baseVal = Hash01((uint)x, (uint)y, 0u, channelSeed);
                    var jitter = (Hash01((uint)x, (uint)y, (uint)(z + 1), channelSeed) - 0.5f) * (0.5f / depth);
                    slicePtr[y * size + x] = math.frac(baseVal + z * goldenConjugate + jitter);
                }
            }
        });

        for (var iter = 0; iter < ITERATION_COUNT; iter++)
        {
            ConvolveHorizontal<TFloat>(pChannel, pTempX, pSpatialKernel, size, depth);
            ConvolveVertical<TFloat>(pTempX, pSpatialBlur, pSpatialKernel, size, depth);
            EvaluateHighPass<TFloat>(pChannel, pSpatialBlur, pHighPass, pTemporalKernel, size, depth);
            EqualizeTemporalRanks(pHighPass, size, depth);
            EqualizeSpatialRanks(pHighPass, pChannel, pSortKeys, pSortIndices, size, depth, channelSeed, iter);
        }
    }

    public static Task GenerateAsync(int size, int depth, string output, CancellationToken cancellationToken)
    {
        if ((size & (size - 1)) != 0 || size < 16)
        {
            throw new ArgumentException("STBN size must be a power of two and at least 16.", nameof(size));
        }

        if ((depth & (depth - 1)) != 0 || depth < 8)
        {
            throw new ArgumentException("STBN depth must be a power of two and at least 8.", nameof(depth));
        }

        var totalVoxels = size * size * depth;

        using var channel0 = new UnsafeArray<float>(totalVoxels, AllocationHandle.TLSF);
        using var channel1 = new UnsafeArray<float>(totalVoxels, AllocationHandle.TLSF);
        using var tempX = new UnsafeArray<float>(totalVoxels, AllocationHandle.TLSF);
        using var spatialBlur = new UnsafeArray<float>(totalVoxels, AllocationHandle.TLSF);
        using var highPass = new UnsafeArray<float>(totalVoxels, AllocationHandle.TLSF);
        using var sortKeys = new UnsafeArray<float>(totalVoxels, AllocationHandle.TLSF);
        using var sortIndices = new UnsafeArray<int>(totalVoxels, AllocationHandle.TLSF);

        var spatialKernelLen = SPATIAL_RADIUS * 2 + 1;
        var temporalKernelLen = TEMPORAL_RADIUS * 2 + 1;
        using var spatialKernel = new UnsafeArray<float>(spatialKernelLen, AllocationHandle.TLSF);
        using var temporalKernel = new UnsafeArray<float>(temporalKernelLen, AllocationHandle.TLSF);

        var spatialSum = 0.0f;
        for (var i = -SPATIAL_RADIUS; i <= SPATIAL_RADIUS; i++)
        {
            var w = math.exp(-(i * i) / (2.0f * SPATIAL_SIGMA * SPATIAL_SIGMA));
            spatialKernel[i + SPATIAL_RADIUS] = w;
            spatialSum += w;
        }

        for (var i = 0; i < spatialKernelLen; i++)
        {
            spatialKernel[i] /= spatialSum;
        }

        var temporalSum = 0.0f;
        for (var i = -TEMPORAL_RADIUS; i <= TEMPORAL_RADIUS; i++)
        {
            if (i == 0)
            {
                temporalKernel[i + TEMPORAL_RADIUS] = spatialKernel[SPATIAL_RADIUS] * spatialKernel[SPATIAL_RADIUS];
                continue;
            }

            var w = math.exp(-(i * i) / (2.0f * TEMPORAL_SIGMA * TEMPORAL_SIGMA));
            temporalKernel[i + TEMPORAL_RADIUS] = w;
            temporalSum += w;
        }

        for (var i = -TEMPORAL_RADIUS; i <= TEMPORAL_RADIUS; i++)
        {
            if (i != 0)
            {
                temporalKernel[i + TEMPORAL_RADIUS] /= temporalSum;
            }
        }

        var pCh0 = (float*)channel0.GetUnsafePtr();
        var pCh1 = (float*)channel1.GetUnsafePtr();
        var pTempX = (float*)tempX.GetUnsafePtr();
        var pSpatialBlur = (float*)spatialBlur.GetUnsafePtr();
        var pHighPass = (float*)highPass.GetUnsafePtr();
        var pSortKeys = (float*)sortKeys.GetUnsafePtr();
        var pSortIndices = (int*)sortIndices.GetUnsafePtr();
        var pSpatialKernel = (float*)spatialKernel.GetUnsafePtr();
        var pTemporalKernel = (float*)temporalKernel.GetUnsafePtr();

        if (WideLane.IsSupported)
        {
            GenerateChannel<WideLane<float>>(pCh0, pTempX, pSpatialBlur, pHighPass, pSortKeys, pSortIndices, pSpatialKernel, pTemporalKernel, size, depth, 0u);
            GenerateChannel<WideLane<float>>(pCh1, pTempX, pSpatialBlur, pHighPass, pSortKeys, pSortIndices, pSpatialKernel, pTemporalKernel, size, depth, 1u);
        }
        else
        {
            GenerateChannel<ScalarLane<float>>(pCh0, pTempX, pSpatialBlur, pHighPass, pSortKeys, pSortIndices, pSpatialKernel, pTemporalKernel, size, depth, 0u);
            GenerateChannel<ScalarLane<float>>(pCh1, pTempX, pSpatialBlur, pHighPass, pSortKeys, pSortIndices, pSpatialKernel, pTemporalKernel, size, depth, 1u);
        }

        using var rg16Data = new UnsafeArray<Half>(totalVoxels * 2, AllocationHandle.TLSF);
        for (var i = 0; i < totalVoxels; i++)
        {
            rg16Data[i * 2] = (Half)pCh0[i];
            rg16Data[i * 2 + 1] = (Half)pCh1[i];
        }

        DDSUtility.WriteDdsR16G16F(output, size, size * depth, rg16Data);

        return Task.CompletedTask;
    }
}
